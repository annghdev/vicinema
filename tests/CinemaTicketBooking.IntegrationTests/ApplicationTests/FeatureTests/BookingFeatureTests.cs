using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Application.Features.Bookings.Commands;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.IntegrationTests.Shared.DataSeeders;
using CinemaTicketBooking.IntegrationTests.Shared.Fakes;
using CinemaTicketBooking.IntegrationTests.Shared.Fixtures;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CinemaTicketBooking.IntegrationTests.ApplicationTests.FeatureTests;

public sealed class BookingFeatureTests(PostgresContainerFixture databaseFixture)
    : ApplicationFeatureTestBase(databaseFixture)
{
    [Fact]
    public async Task ValidatePreCheckoutSeatSelection_Should_Block_When_OrphanSeatIsCreated()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var response = await InvokeAsync<ValidateSeatSelectionResponse>(new ValidateSeatSelectionCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            SelectedTicketIds =
            [
                seed.TicketsBySeatCode["A1"],
                seed.TicketsBySeatCode["A2"],
                seed.TicketsBySeatCode["A4"],
                seed.TicketsBySeatCode["A5"]
            ],
            CorrelationId = "it-precheckout-orphan"
        });

        response.CanProceed.Should().BeFalse();
        response.Errors.Should().Contain(x => x.Type == "ORPHAN_SEAT");
    }

    [Fact]
    public async Task ValidatePreCheckoutSeatSelection_Should_Pass_When_ContiguousSelection()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var response = await InvokeAsync<ValidateSeatSelectionResponse>(new ValidateSeatSelectionCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            SelectedTicketIds =
            [
                seed.TicketsBySeatCode["A1"],
                seed.TicketsBySeatCode["A2"],
                seed.TicketsBySeatCode["A3"]
            ],
            CorrelationId = "it-precheckout-ok"
        });

        response.CanProceed.Should().BeTrue();
        response.Errors.Should().BeEmpty();
        response.PaymentOptions.Should().NotBeNullOrEmpty();
        response.PaymentOptions!.Should().Contain(x => x.Method == "None");
    }

    [Fact]
    public async Task CreateBooking_Should_UseSharedPaymentExpiry_ForAllSelectedTickets_And_CreatePaymentTransaction()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();
        var concessionId = await SeedConcessionAsync();

        var response = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            CustomerName = "Guest Checkout",
            CustomerPhoneNumber = "0123456789",
            CustomerEmail = "guest.checkout@example.com",
            SelectedTicketIds =
            [
                seed.TicketsBySeatCode["A1"],
                seed.TicketsBySeatCode["A2"]
            ],
            Concessions = [new CheckoutConcessionSelection(concessionId, 2)],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-create-booking-process-payment"
        });

        await using var db = CreateDbContext();
        var booking = await db.Bookings
            .Include(x => x.Tickets)
            .ThenInclude(x => x.Ticket)
            .SingleAsync(x => x.Id == response.BookingId);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Tickets.Should().HaveCount(2);
        booking.Tickets.Select(x => x.Ticket!.Status).Should().OnlyContain(x => x == TicketStatus.PendingPayment);
        booking.Tickets
            .Select(x => x.Ticket!.PaymentExpiresAt)
            .Distinct()
            .Should()
            .ContainSingle();
        booking.Tickets.First().Ticket!.PaymentExpiresAt
            .Should()
            .BeCloseTo(response.PaymentExpiresAt, precision: TimeSpan.FromMilliseconds(10));
        response.PaymentStatus.Should().Be("pending_payment");
        response.FinalAmount.Should().Be(booking.FinalAmount);
        response.PaymentUrl.Should().NotBeNullOrEmpty();
        response.RedirectBehavior.Should().Be(PaymentRedirectBehavior.QrCode);
        response.PaymentTransactionId.Should().NotBeNull();

        var paymentTransaction = await db.PaymentTransactions
            .SingleAsync(x => x.BookingId == response.BookingId);
        paymentTransaction.Method.Should().Be(PaymentMethod.None);
        paymentTransaction.Status.Should().Be(PaymentTransactionStatus.Pending);
        paymentTransaction.Amount.Should().Be(booking.FinalAmount);
    }

    [Fact]
    public async Task VerifyPayment_Should_ConfirmBooking_SetQrAndSoldTickets_When_GatewayConfirms()
    {
        var (bookingId, gatewayTxId, paymentTxId, _) = await ArrangePendingBookingForVerifyPaymentAsync();
        var paymentRealtime = ResolveRequiredService<InMemoryPaymentRealtimePublisher>();
        paymentRealtime.Reset();

        var verifyResponse = await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = [],
            CorrelationId = "it-verify-payment-success"
        });

        verifyResponse.IsSuccess.Should().BeTrue();
        verifyResponse.Status.Should().Be("confirmed");
        verifyResponse.PaymentTransactionId.Should().Be(paymentTxId);
        verifyResponse.CheckinQrCode.Should().NotBeNullOrEmpty();
        verifyResponse.CheckinQrCode!.Should().Be(bookingId.ToString());
        verifyResponse.CheckinQrCode!.Length.Should().BeLessOrEqualTo(MaxLengthConsts.QrCode);
        verifyResponse.CanRetry.Should().BeFalse();

        await using var db = CreateDbContext();
        var bookingAfter = await db.Bookings
            .Include(x => x.Tickets)
            .ThenInclude(x => x.Ticket)
            .SingleAsync(x => x.Id == bookingId);

        bookingAfter.Status.Should().Be(BookingStatus.Confirmed);
        bookingAfter.QrCode.Should().Be(verifyResponse.CheckinQrCode);
        bookingAfter.Tickets.Should().ContainSingle();
        bookingAfter.Tickets.Single().Ticket!.Status.Should().Be(TicketStatus.Sold);

        var paymentAfter = await db.PaymentTransactions.SingleAsync(x => x.Id == paymentTxId);
        paymentAfter.Status.Should().Be(PaymentTransactionStatus.Success);
        paymentAfter.PaidAt.Should().NotBeNull();

        paymentRealtime.Events.Should().ContainSingle();
        var realtimeEvent = paymentRealtime.Events.Single();
        realtimeEvent.BookingId.Should().Be(bookingId);
        realtimeEvent.PaymentTransactionId.Should().Be(paymentTxId);
        realtimeEvent.Status.Should().Be("confirmed");
        realtimeEvent.CheckinQrCode.Should().Be(verifyResponse.CheckinQrCode);
    }

    [Fact]
    public async Task VerifyPayment_Should_Throw_When_GatewayTransactionId_DoesNotExist()
    {
        var (bookingId, gatewayTxId, _, _) = await ArrangePendingBookingForVerifyPaymentAsync();

        var act = async () => await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = "WRONG-" + gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = [],
            CorrelationId = "it-verify-payment-mismatch"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*was not found*");
    }

    [Fact]
    public async Task VerifyPayment_Should_Return_CurrentState_When_Transaction_Already_Success()
    {
        var (bookingId, gatewayTxId, paymentTxId, _) = await ArrangePendingBookingForVerifyPaymentAsync();

        await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = [],
            CorrelationId = "it-verify-payment-first-success"
        });

        var secondCall = await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = [],
            CorrelationId = "it-verify-payment-second-success"
        });

        secondCall.IsSuccess.Should().BeTrue();
        secondCall.Status.Should().Be("confirmed");
        secondCall.PaymentTransactionId.Should().Be(paymentTxId);
        secondCall.CanRetry.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyPayment_Should_MarkTransactionFailed_And_KeepBookingPending_When_CallbackSignatureInvalid()
    {
        var (bookingId, gatewayTxId, paymentTxId, _) = await ArrangePendingBookingForVerifyPaymentAsync();

        var verifyResponse = await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = new Dictionary<string, string>
            {
                ["vnp_ResponseCode"] = "00",
                ["vnp_TxnRef"] = gatewayTxId,
                ["vnp_SecureHash"] = "deadbeef"
            },
            CorrelationId = "it-verify-payment-bad-signature"
        });

        verifyResponse.IsSuccess.Should().BeFalse();
        verifyResponse.Status.Should().Be("payment_failed");
        verifyResponse.CanRetry.Should().BeTrue();
        verifyResponse.CheckinQrCode.Should().BeNull();
        verifyResponse.AvailableGateways.Should().NotBeNullOrEmpty();
        verifyResponse.ErrorMessage.Should().Contain("signature");

        await using var db = CreateDbContext();
        var bookingAfter = await db.Bookings
            .Include(x => x.Tickets)
            .ThenInclude(x => x.Ticket)
            .SingleAsync(x => x.Id == bookingId);

        bookingAfter.Status.Should().Be(BookingStatus.Pending);
        bookingAfter.QrCode.Should().BeNull();
        bookingAfter.Tickets.Single().Ticket!.Status.Should().Be(TicketStatus.PendingPayment);

        var paymentAfter = await db.PaymentTransactions.SingleAsync(x => x.Id == paymentTxId);
        paymentAfter.Status.Should().Be(PaymentTransactionStatus.Failed);
    }

    [Fact]
    public async Task VerifyPayment_Should_ConfirmBooking_When_MomoSignatureIsValid()
    {
        var (bookingId, _, paymentTxId, _) = await ArrangePendingBookingForVerifyPaymentAsync();

        await using (var db = CreateDbContext())
        {
            var tx = await db.PaymentTransactions.SingleAsync(x => x.Id == paymentTxId);
            tx.Method = PaymentMethod.Momo;
            tx.GatewayTransactionId = $"BOOKING-{bookingId:N}".ToUpperInvariant();
            tx.PaymentUrl = "https://test-payment.momo.vn";
            db.PaymentTransactions.Update(tx);
            await db.SaveChangesAsync();
        }

        var callbackFields = new Dictionary<string, string>
        {
            ["partnerCode"] = "MOMO_TEST_PARTNER",
            ["requestId"] = "MOMO-REQ-1",
            ["orderId"] = $"BOOKING-{bookingId:N}".ToUpperInvariant(),
            ["orderInfo"] = $"Booking {bookingId}",
            ["orderType"] = "momo_wallet",
            ["amount"] = "100000",
            ["resultCode"] = "0",
            ["message"] = "Success",
            ["payType"] = "qr",
            ["transId"] = "123456",
            ["responseTime"] = "1710000000000",
            ["extraData"] = ""
        };
        callbackFields["signature"] = ComputeMomoSignature(callbackFields, "MOMO_TEST_SECRET_KEY");

        var verifyResponse = await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = callbackFields["orderId"],
            PaymentMethod = "Momo",
            GatewayResponseParams = callbackFields,
            CorrelationId = "it-verify-payment-momo-success"
        });

        verifyResponse.IsSuccess.Should().BeTrue();
        verifyResponse.Status.Should().Be("confirmed");
        verifyResponse.PaymentTransactionId.Should().Be(paymentTxId);
        verifyResponse.CheckinQrCode.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RetryPayment_Should_CreateNewPendingTransaction_When_PreviousVerificationFailed()
    {
        var (bookingId, sessionId) = await ArrangeBookingWithFailedPaymentAsync();

        var retryResponse = await InvokeAsync<CreateBookingResponse>(new RetryPaymentCommand
        {
            BookingId = bookingId,
            CustomerSessionId = sessionId,
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-retry-payment-after-fail"
        });

        retryResponse.BookingId.Should().Be(bookingId);
        retryResponse.PaymentStatus.Should().Be("pending_payment");
        retryResponse.PaymentUrl.Should().NotBeNullOrEmpty();
        retryResponse.PaymentTransactionId.Should().NotBeNull();
        retryResponse.RedirectBehavior.Should().Be(PaymentRedirectBehavior.QrCode);

        await using var db = CreateDbContext();
        var transactions = await db.PaymentTransactions
            .Where(x => x.BookingId == bookingId)
            .ToListAsync();

        transactions.Should().HaveCount(2);
        transactions.Should().ContainSingle(x => x.Status == PaymentTransactionStatus.Failed);
        var pendingRetry = transactions.Single(x => x.Status == PaymentTransactionStatus.Pending);
        pendingRetry.Id.Should().Be(retryResponse.PaymentTransactionId!.Value);

        var booking = await db.Bookings
            .Include(x => x.Tickets)
            .ThenInclude(x => x.Ticket)
            .SingleAsync(x => x.Id == bookingId);

        booking.Status.Should().Be(BookingStatus.Pending);
        booking.Tickets.Single().Ticket!.PaymentExpiresAt
            .Should()
            .BeCloseTo(retryResponse.PaymentExpiresAt, precision: TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task RetryPayment_Should_Throw_When_PaymentStillPending()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var createResponse = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            CustomerName = "Retry Blocked",
            CustomerPhoneNumber = "0123456789",
            CustomerEmail = "retry.blocked@example.com",
            SelectedTicketIds = [seed.TicketsBySeatCode["A1"]],
            Concessions = [],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-retry-while-pending"
        });

        var act = async () => await InvokeAsync<CreateBookingResponse>(new RetryPaymentCommand
        {
            BookingId = createResponse.BookingId,
            CustomerSessionId = seed.SessionId,
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-retry-while-pending-2"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already in progress*");
    }

    [Fact]
    public async Task RetryPayment_Should_Cancel_CurrentPendingTransaction_When_UserSwitchesGateway()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var createResponse = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            CustomerName = "Retry Switch Gateway",
            CustomerPhoneNumber = "0123456789",
            CustomerEmail = "retry.switch@example.com",
            SelectedTicketIds = [seed.TicketsBySeatCode["A1"]],
            Concessions = [],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-retry-switch-gateway-initial"
        });

        var retryResponse = await InvokeAsync<CreateBookingResponse>(new RetryPaymentCommand
        {
            BookingId = createResponse.BookingId,
            CustomerSessionId = seed.SessionId,
            PaymentMethod = "VnPay",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            ReplacePendingPayment = true,
            CorrelationId = "it-retry-switch-gateway"
        });

        await using var db = CreateDbContext();
        var transactions = await db.PaymentTransactions
            .Where(x => x.BookingId == createResponse.BookingId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        transactions.Should().HaveCount(2);
        transactions.Should().ContainSingle(x => x.Status == PaymentTransactionStatus.Cancelled);
        transactions.Should().ContainSingle(x => x.Status == PaymentTransactionStatus.Pending);
        transactions.Single(x => x.Status == PaymentTransactionStatus.Pending).Method.Should().Be(PaymentMethod.VnPay);
        retryResponse.PaymentTransactionId.Should().Be(transactions.Single(x => x.Status == PaymentTransactionStatus.Pending).Id);
    }

    [Fact]
    public async Task CancelBooking_Should_ReleaseTickets_And_CancelPendingPaymentTransactions()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var createResponse = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            CustomerName = "Cancel Booking",
            CustomerPhoneNumber = "0123456789",
            CustomerEmail = "cancel.booking@example.com",
            SelectedTicketIds = [seed.TicketsBySeatCode["A1"], seed.TicketsBySeatCode["A2"]],
            Concessions = [],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-cancel-booking-setup"
        });

        await InvokeAsync(new CancelBookingCommand
        {
            BookingId = createResponse.BookingId,
            CorrelationId = "it-cancel-booking"
        });

        await using var db = CreateDbContext();
        var booking = await db.Bookings
            .Include(x => x.Tickets)
            .ThenInclude(x => x.Ticket)
            .SingleAsync(x => x.Id == createResponse.BookingId);

        booking.Status.Should().Be(BookingStatus.Cancelled);
        booking.Tickets.Should().OnlyContain(x => x.Ticket != null && x.Ticket.Status == TicketStatus.Available);

        var pendingTransactions = await db.PaymentTransactions
            .Where(x => x.BookingId == createResponse.BookingId)
            .ToListAsync();
        pendingTransactions.Should().ContainSingle();
        pendingTransactions.Single().Status.Should().Be(PaymentTransactionStatus.Cancelled);
    }

    [Fact]
    public async Task RetryPayment_Should_Throw_When_CustomerSessionDoesNotMatch()
    {
        var (bookingId, _) = await ArrangeBookingWithFailedPaymentAsync();

        var act = async () => await InvokeAsync<CreateBookingResponse>(new RetryPaymentCommand
        {
            BookingId = bookingId,
            CustomerSessionId = "another-session",
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-retry-wrong-session"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task GetBookingById_Should_Return_ExpectedBookingDetails_When_BypassingAuthorizationWithFakePermission()
    {
        await ResetDatabaseAsync();
        var seed = await SeedBookingReadGraphAsync();
        ConfigureFakeUserContext(Guid.CreateVersion7(), Permissions.BookingsViewAll);

        var result = await InvokeAsync<BookingDetailsDto?>(new GetBookingByIdQuery
        {
            BookingId = seed.PrimaryBookingId,
            CorrelationId = "it-get-booking-by-id-read"
        });

        result.Should().NotBeNull();
        result!.BookingId.Should().Be(seed.PrimaryBookingId);
        result.ShowTimeInfo.Screen.Should().Be("READ-SCR-1");
        result.ShowTimeInfo.Movie.Should().Be("Read Query Movie");
        result.OriginalAmount.Should().Be(370_000m);
        result.FinalAmount.Should().Be(350_000m);
        result.CheckinQrCode.Should().Be("qr://booking/read");
        result.Tickets.Should().HaveCount(2);
        result.Tickets.Select(x => x.SeatCode).Should().BeEquivalentTo(["A1", "A2"]);
        result.Concessions.Should().ContainSingle();
        result.Concessions.Single().Name.Should().Be("Read Combo");
        result.Concessions.Single().Quantity.Should().Be(1);
        result.Concessions.Single().Amount.Should().Be(120_000m);
    }

    [Fact]
    public async Task GetBookingHistoryByCustomerId_Should_Return_FilteredPagedData_When_BypassingAuthorizationWithFakePermission()
    {
        await ResetDatabaseAsync();
        var seed = await SeedBookingHistoryGraphAsync();
        ConfigureFakeUserContext(Guid.CreateVersion7(), Permissions.BookingsViewAll);

        var page1 = await InvokeAsync<PagedResult<BookingMinimalInfoDto>>(new GetBookingHistoryByCustomerIdQuery
        {
            CustomerId = seed.OwnerCustomerId,
            Date = seed.FilterDate,
            PageNumber = 1,
            PageSize = 1,
            CorrelationId = "it-get-booking-history-page-1"
        });

        page1.TotalItems.Should().Be(2);
        page1.PageNumber.Should().Be(1);
        page1.PageSize.Should().Be(1);
        page1.TotalPages.Should().Be(2);
        page1.Items.Should().ContainSingle();
        page1.Items.Single().BookingId.Should().Be(seed.NewestBookingIdOnFilterDate);

        var page2 = await InvokeAsync<PagedResult<BookingMinimalInfoDto>>(new GetBookingHistoryByCustomerIdQuery
        {
            CustomerId = seed.OwnerCustomerId,
            Date = seed.FilterDate,
            PageNumber = 2,
            PageSize = 1,
            CorrelationId = "it-get-booking-history-page-2"
        });

        page2.TotalItems.Should().Be(2);
        page2.Items.Should().ContainSingle();
        page2.Items.Single().BookingId.Should().Be(seed.OldestBookingIdOnFilterDate);
    }

    private void ConfigureFakeUserContext(Guid? customerId, params string[] permissions)
    {
        FakeUserContext.IsAuthenticated = true;
        FakeUserContext.CustomerId = customerId;
        FakeUserContext.Permissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }

    private async Task<(Guid BookingId, string SessionId)> ArrangeBookingWithFailedPaymentAsync()
    {
        var (bookingId, gatewayTxId, _, sessionId) = await ArrangePendingBookingForVerifyPaymentAsync();

        await InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = gatewayTxId,
            PaymentMethod = "None",
            GatewayResponseParams = new Dictionary<string, string>
            {
                ["vnp_ResponseCode"] = "00",
                ["vnp_SecureHash"] = "invalid"
            },
            CorrelationId = "it-arrange-failed-payment"
        });

        return (bookingId, sessionId);
    }

    private async Task<(Guid BookingId, string GatewayTransactionId, Guid PaymentTransactionId, string SessionId)>
        ArrangePendingBookingForVerifyPaymentAsync()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCheckoutGraphAsync();

        var createResponse = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = seed.SessionId,
            CustomerName = "Verify Payment IT",
            CustomerPhoneNumber = "0123456789",
            CustomerEmail = "verify.payment.it@example.com",
            SelectedTicketIds = [seed.TicketsBySeatCode["A1"]],
            Concessions = [],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-arrange-pending-booking"
        });

        await using var db = CreateDbContext();
        var transaction = await db.PaymentTransactions
            .AsNoTracking()
            .SingleAsync(x => x.BookingId == createResponse.BookingId);

        return (createResponse.BookingId, transaction.GatewayTransactionId, transaction.Id, seed.SessionId);
    }

    private async Task<(Guid ShowTimeId, string SessionId, Dictionary<string, Guid> TicketsBySeatCode)> SeedCheckoutGraphAsync()
    {
        await using var db = CreateDbContext();

        var cinema = IntegrationEntityBuilder.Cinema("Checkout Cinema");
        var movie = IntegrationEntityBuilder.Movie("Checkout Movie", MovieStatus.NowShowing);
        var screen = IntegrationEntityBuilder.Screen(cinema.Id, "CHK-SCR-1", "[[1,1,1,1,1]]");
        var showTime = IntegrationEntityBuilder.ShowTime(movie.Id, screen.Id);

        var sessionId = "checkout-session-1";
        var seatCodes = new[] { "A1", "A2", "A3", "A4", "A5" };
        var tickets = seatCodes.Select(seatCode => new Ticket
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = showTime.Id,
            SeatId = screen.Seats.FirstOrDefault(x => x.Code == seatCode)?.Id,
            SeatCode = seatCode,
            Status = TicketStatus.Locking,
            LockingBy = sessionId,
            LockExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Code = $"20260415-{screen.Code}-{seatCode}",
            Description = $"{seatCode} - Regular",
            Price = 100_000m
        }).ToList();

        db.Cinemas.Add(cinema);
        db.Movies.Add(movie);
        db.Screens.Add(screen);
        db.ShowTimes.Add(showTime);
        db.Tickets.AddRange(tickets);

        await db.SaveChangesAsync();

        return (
            showTime.Id,
            sessionId,
            tickets.ToDictionary(
                x => x.SeatCode,
                x => x.Id));
    }

    private static string ComputeMomoSignature(
        Dictionary<string, string> fields,
        string secret)
    {
        var orderedKeys = new[]
        {
            "accessKey",
            "amount",
            "extraData",
            "message",
            "orderId",
            "orderInfo",
            "orderType",
            "partnerCode",
            "payType",
            "requestId",
            "responseTime",
            "resultCode",
            "transId"
        };

        var raw = string.Join("&", orderedKeys
            .Select(k =>
            {
                if (k == "accessKey")
                    return $"{k}=MOMO_TEST_ACCESS_KEY";
                return $"{k}={(fields.TryGetValue(k, out var value) ? value : string.Empty)}";
            }));
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(raw);
        return Convert.ToHexStringLower(HMACSHA256.HashData(keyBytes, dataBytes));
    }

    private async Task<Guid> SeedConcessionAsync()
    {
        await using var db = CreateDbContext();
        var concession = IntegrationEntityBuilder.Concession("Big Combo", 80_000m);
        db.Concessions.Add(concession);
        await db.SaveChangesAsync();
        return concession.Id;
    }

    private async Task<(Guid PrimaryBookingId, Guid OwnerCustomerId)> SeedBookingReadGraphAsync()
    {
        await using var db = CreateDbContext();

        var cinema = IntegrationEntityBuilder.Cinema("Read Query Cinema");
        var movie = IntegrationEntityBuilder.Movie("Read Query Movie", MovieStatus.NowShowing);
        var screen = IntegrationEntityBuilder.Screen(cinema.Id, "READ-SCR-1", "[[1,1,1]]");
        var showTime = IntegrationEntityBuilder.ShowTime(movie.Id, screen.Id);
        showTime.Date = new DateOnly(2026, 4, 16);

        var owner = IntegrationEntityBuilder.Customer("session-owner");
        owner.IsRegistered = true;
        var otherCustomer = IntegrationEntityBuilder.Customer("session-other");
        otherCustomer.IsRegistered = true;

        var ticketA1 = IntegrationEntityBuilder.Ticket(showTime.Id, "READ-T-1", TicketStatus.Sold);
        ticketA1.SeatCode = "A1";
        ticketA1.Price = 125_000m;
        var ticketA2 = IntegrationEntityBuilder.Ticket(showTime.Id, "READ-T-2", TicketStatus.Sold);
        ticketA2.SeatCode = "A2";
        ticketA2.Price = 125_000m;

        var concession = IntegrationEntityBuilder.Concession("Read Combo", 120_000m);
        concession.ImageUrl = "https://img.local/read-combo.png";

        var booking = IntegrationEntityBuilder.Booking(showTime.Id, owner.Id, "Read Owner");
        booking.CreatedAt = new DateTimeOffset(2026, 4, 16, 12, 0, 0, TimeSpan.Zero);
        booking.OriginAmount = 370_000m;
        booking.FinalAmount = 350_000m;
        booking.Status = BookingStatus.Confirmed;
        booking.QrCode = "qr://booking/read";
        booking.Tickets =
        [
            new BookingTicket
            {
                Id = Guid.CreateVersion7(),
                BookingId = booking.Id,
                TicketId = ticketA1.Id,
                Ticket = ticketA1
            },
            new BookingTicket
            {
                Id = Guid.CreateVersion7(),
                BookingId = booking.Id,
                TicketId = ticketA2.Id,
                Ticket = ticketA2
            }
        ];
        booking.Concessions =
        [
            new BookingConcession
            {
                Id = Guid.CreateVersion7(),
                BookingId = booking.Id,
                ConcessionId = concession.Id,
                Concession = concession,
                Quantity = 1
            }
        ];

        var otherBooking = IntegrationEntityBuilder.Booking(showTime.Id, otherCustomer.Id, "Other Owner");
        otherBooking.OriginAmount = 100_000m;
        otherBooking.FinalAmount = 100_000m;
        otherBooking.QrCode = "qr://booking/other";

        db.Cinemas.Add(cinema);
        db.Movies.Add(movie);
        db.Screens.Add(screen);
        db.ShowTimes.Add(showTime);
        db.Customers.AddRange(owner, otherCustomer);
        db.Concessions.Add(concession);
        db.Tickets.AddRange(ticketA1, ticketA2);
        db.Bookings.AddRange(booking, otherBooking);

        await db.SaveChangesAsync();
        return (booking.Id, owner.Id);
    }

    private async Task<(Guid OwnerCustomerId, Guid NewestBookingIdOnFilterDate, Guid OldestBookingIdOnFilterDate, DateOnly FilterDate)>
        SeedBookingHistoryGraphAsync()
    {
        await using var db = CreateDbContext();

        var cinema = IntegrationEntityBuilder.Cinema("History Query Cinema");
        var movie = IntegrationEntityBuilder.Movie("History Query Movie", MovieStatus.NowShowing);
        var screen = IntegrationEntityBuilder.Screen(cinema.Id, "HIS-SCR-1", "[[1,1,1]]");

        var filterDate = new DateOnly(2026, 4, 17);
        var sameDayStart = new DateTimeOffset(2026, 4, 17, 18, 0, 0, TimeSpan.Zero);
        var otherDayStart = new DateTimeOffset(2026, 4, 18, 9, 0, 0, TimeSpan.Zero);

        var showTimeOnFilterDate = IntegrationEntityBuilder.ShowTime(movie.Id, screen.Id);
        showTimeOnFilterDate.Date = filterDate;
        showTimeOnFilterDate.StartAt = sameDayStart;
        showTimeOnFilterDate.EndAt = sameDayStart.AddHours(2);

        var showTimeOnOtherDate = IntegrationEntityBuilder.ShowTime(movie.Id, screen.Id);
        showTimeOnOtherDate.Date = DateOnly.FromDateTime(otherDayStart.DateTime);
        showTimeOnOtherDate.StartAt = otherDayStart;
        showTimeOnOtherDate.EndAt = otherDayStart.AddHours(2);

        var owner = IntegrationEntityBuilder.Customer("history-owner");
        owner.IsRegistered = true;
        var otherCustomer = IntegrationEntityBuilder.Customer("history-other");
        otherCustomer.IsRegistered = true;

        var oldestBooking = IntegrationEntityBuilder.Booking(showTimeOnFilterDate.Id, owner.Id, "History Owner");
        oldestBooking.CreatedAt = new DateTimeOffset(2026, 4, 17, 8, 0, 0, TimeSpan.Zero);
        oldestBooking.FinalAmount = 210_000m;
        oldestBooking.OriginAmount = 210_000m;

        var newestBooking = IntegrationEntityBuilder.Booking(showTimeOnFilterDate.Id, owner.Id, "History Owner");
        newestBooking.CreatedAt = new DateTimeOffset(2026, 4, 17, 10, 0, 0, TimeSpan.Zero);
        newestBooking.FinalAmount = 260_000m;
        newestBooking.OriginAmount = 260_000m;

        var bookingOnOtherDay = IntegrationEntityBuilder.Booking(showTimeOnOtherDate.Id, owner.Id, "History Owner");
        bookingOnOtherDay.CreatedAt = new DateTimeOffset(2026, 4, 18, 7, 0, 0, TimeSpan.Zero);
        bookingOnOtherDay.FinalAmount = 180_000m;
        bookingOnOtherDay.OriginAmount = 180_000m;

        var bookingOfOtherCustomer = IntegrationEntityBuilder.Booking(showTimeOnFilterDate.Id, otherCustomer.Id, "History Other");
        bookingOfOtherCustomer.CreatedAt = new DateTimeOffset(2026, 4, 17, 9, 0, 0, TimeSpan.Zero);
        bookingOfOtherCustomer.FinalAmount = 150_000m;
        bookingOfOtherCustomer.OriginAmount = 150_000m;

        db.Cinemas.Add(cinema);
        db.Movies.Add(movie);
        db.Screens.Add(screen);
        db.ShowTimes.AddRange(showTimeOnFilterDate, showTimeOnOtherDate);
        db.Customers.AddRange(owner, otherCustomer);
        db.Bookings.AddRange(oldestBooking, newestBooking, bookingOnOtherDay, bookingOfOtherCustomer);

        await db.SaveChangesAsync();
        return (owner.Id, newestBooking.Id, oldestBooking.Id, filterDate);
    }
}

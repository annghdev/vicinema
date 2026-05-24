using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Application.Features.Bookings.Commands;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Infrastructure.Payments.Vnpay;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/payments")
            .RequireRateLimiting("sliding")
            .WithTags("Payments");

        group.MapGet("/vnpay-return", VnpayReturn)
            .AllowAnonymous();

        group.MapGet("/momo-return", MomoReturn)
            .AllowAnonymous();

        group.MapPost("/momo-ipn", MomoIpn)
            .AllowAnonymous();

        group.MapGet("/fake-callback", FakeCallback)
            .AllowAnonymous();

        group.MapGet("/vnpay-ipn", VnpayIpn)
            .AllowAnonymous();

        group.MapGet("/result", GetPaymentResult)
            .AllowAnonymous();

        group.MapGet("/gateways", GetAvailableGateways)
            .AllowAnonymous();
    }

    /// <summary>
    /// VNPay return URL: browser is redirected here after payment.
    /// If IPN has not yet arrived (transaction still Pending), performs inline
    /// verification as a fallback so the user is not stuck waiting.
    /// Frontend contract: ?status=success|pending|failed &amp;bookingId=...&amp;txnRef=...
    /// </summary>
    public static async Task<IResult> VnpayReturn(
        HttpRequest request,
        IConfiguration configuration,
        IUnitOfWork uow,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        var frontendOrigin = configuration["FrontendOrigin"] ?? request.Scheme + "://" + request.Host;

        // 1. Extract VNPay transaction reference.
        var txnRef = request.Query["vnp_TxnRef"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return BuildFailedRedirect(frontendOrigin, txnRef: null, bookingId: null, "Ma giao dich khong hop le.");
        }

        // 2. Look up payment transaction by gateway txnRef.
        var transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default);
        if (transaction is null)
        {
            return BuildFailedRedirect(frontendOrigin, txnRef, bookingId: null, "Giao dich khong ton tai.");
        }

        // 3. Fallback verification when IPN has not arrived yet.
        if (transaction.Status == PaymentTransactionStatus.Pending)
        {
            logger.LogInformation("[VnpayReturn] Transaction {TxnRef} still Pending — running inline verification as IPN fallback.", txnRef);

            var gatewayParams = request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            try
            {
                await bus.InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
                {
                    BookingId = transaction.BookingId,
                    GatewayTransactionId = txnRef,
                    PaymentMethod = PaymentMethod.VnPay.ToString(),
                    GatewayResponseParams = gatewayParams
                });

                // Re-read transaction after verification to get updated status.
                transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default)
                              ?? transaction;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[VnpayReturn] Inline verification failed for {TxnRef}.", txnRef);
            }
        }

        // 4. Build normalized redirect based on authoritative DB status.
        return BuildGatewayRedirect(frontendOrigin, transaction, txnRef);
    }

    /// <summary>
    /// MoMo return URL: browser is redirected here after payment.
    /// If IPN has not yet arrived (transaction still Pending), performs inline
    /// verification as a fallback so the user is not stuck waiting.
    /// Frontend contract: ?status=success|pending|failed &amp;bookingId=...&amp;txnRef=...
    /// </summary>
    public static async Task<IResult> MomoReturn(
        HttpRequest request,
        IConfiguration configuration,
        IUnitOfWork uow,
        IMessageBus bus,
        ILogger<Program> logger)
    {
        var frontendOrigin = configuration["FrontendOrigin"] ?? request.Scheme + "://" + request.Host;

        // 1. Extract MoMo orderId (acts as the transaction reference).
        var txnRef = request.Query["orderId"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return BuildFailedRedirect(frontendOrigin, txnRef: null, bookingId: null, "Ma don hang khong hop le.");
        }

        // 2. Look up payment transaction.
        var transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default);
        if (transaction is null)
        {
            return BuildFailedRedirect(frontendOrigin, txnRef, bookingId: null, "Giao dich khong ton tai.");
        }

        // 3. Fallback verification when IPN has not arrived yet.
        if (transaction.Status == PaymentTransactionStatus.Pending)
        {
            logger.LogInformation("[MomoReturn] Transaction {TxnRef} still Pending — running inline verification as IPN fallback.", txnRef);

            var gatewayParams = request.Query
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            try
            {
                await bus.InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
                {
                    BookingId = transaction.BookingId,
                    GatewayTransactionId = txnRef,
                    PaymentMethod = PaymentMethod.Momo.ToString(),
                    GatewayResponseParams = gatewayParams
                });

                // Re-read transaction after verification to get updated status.
                transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default)
                              ?? transaction;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[MomoReturn] Inline verification failed for {TxnRef}.", txnRef);
            }
        }

        // 4. Build normalized redirect.
        return BuildGatewayRedirect(frontendOrigin, transaction, txnRef);
    }

    // =============================================
    // Shared redirect helpers
    // =============================================

    /// <summary>
    /// Builds a normalized frontend redirect from the transaction's persisted status.
    /// </summary>
    private static IResult BuildGatewayRedirect(string frontendOrigin, PaymentTransaction transaction, string txnRef)
    {
        var bookingId = transaction.BookingId.ToString();

        return transaction.Status switch
        {
            PaymentTransactionStatus.Success =>
                Results.Redirect(
                    $"{frontendOrigin}/payment-result?status=success&bookingId={bookingId}&txnRef={Uri.EscapeDataString(txnRef)}"),

            PaymentTransactionStatus.Pending =>
                // IPN may still be in-flight; frontend will poll /api/payments/result.
                Results.Redirect(
                    $"{frontendOrigin}/payment-result?status=pending&bookingId={bookingId}&txnRef={Uri.EscapeDataString(txnRef)}"),

            _ =>
                BuildFailedRedirect(frontendOrigin, txnRef, bookingId, "Thanh toan khong thanh cong hoac da bi tu choi."),
        };
    }

    /// <summary>
    /// Builds a normalized failure redirect URL.
    /// </summary>
    private static IResult BuildFailedRedirect(
        string frontendOrigin, string? txnRef, string? bookingId, string message)
    {
        var qs = $"status=failed&message={Uri.EscapeDataString(message)}";
        if (!string.IsNullOrWhiteSpace(txnRef)) qs += $"&txnRef={Uri.EscapeDataString(txnRef)}";
        if (!string.IsNullOrWhiteSpace(bookingId)) qs += $"&bookingId={bookingId}";
        return Results.Redirect($"{frontendOrigin}/payment-result?{qs}");
    }

    public static async Task<IResult> FakeCallback(
        HttpRequest request,
        IMessageBus bus,
        [FromQuery] Guid bookingId,
        [FromQuery] string transactionId)
    {
        var gatewayParams = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

        var command = new VerifyPaymentCommand
        {
            BookingId = bookingId,
            GatewayTransactionId = transactionId,
            PaymentMethod = "None",
            GatewayResponseParams = gatewayParams
        };

        var response = await bus.InvokeAsync<VerifyPaymentResponse>(command);

        if (response.IsSuccess)
        {
            return Results.Ok(response);
        }

        return Results.BadRequest(response);
    }

    public static async Task<IResult> MomoIpn(
        HttpRequest request,
        IMessageBus bus,
        IUnitOfWork uow,
        ILogger<Program> logger)
    {
        logger.LogInformation("[MomoIPN] Received IPN callback from {RemoteIp}, ContentType={ContentType}",
            request.HttpContext.Connection.RemoteIpAddress, request.ContentType);

        var gatewayParams = await ReadBodyAsStringDictionaryAsync(request);
        if (!gatewayParams.TryGetValue("orderId", out var orderId) || string.IsNullOrWhiteSpace(orderId))
        {
            return Results.Ok(new MomoIpnResponse(1001, "Invalid request"));
        }

        if (!gatewayParams.TryGetValue("amount", out var amountRaw)
            || !long.TryParse(amountRaw, out var momoAmount))
        {
            return Results.Ok(new MomoIpnResponse(1001, "Invalid amount"));
        }

        var transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(orderId, default);
        if (transaction is null)
        {
            return Results.Ok(new MomoIpnResponse(1002, "Order not found"));
        }

        var expectedAmount = Convert.ToInt64(decimal.Round(transaction.Amount, 0, MidpointRounding.AwayFromZero));
        if (expectedAmount != momoAmount)
        {
            return Results.Ok(new MomoIpnResponse(1003, "Amount mismatch"));
        }

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            return Results.Ok(new MomoIpnResponse(0, "Already processed"));
        }

        try
        {
            var verify = await bus.InvokeAsync<VerifyPaymentResponse>(new VerifyPaymentCommand
            {
                BookingId = transaction.BookingId,
                GatewayTransactionId = orderId,
                PaymentMethod = PaymentMethod.Momo.ToString(),
                GatewayResponseParams = gatewayParams
            });

            if (!verify.IsSuccess && !string.IsNullOrWhiteSpace(verify.ErrorMessage)
                && verify.ErrorMessage.Contains("signature", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(new MomoIpnResponse(1005, "Invalid signature"));
            }

            // IPN is acknowledgment-oriented: once processed, return success to stop retries.
            return Results.Ok(new MomoIpnResponse(0, "Success"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new MomoIpnResponse(1005, "Invalid signature"));
        }
        catch
        {
            return Results.Ok(new MomoIpnResponse(99, "Unknown error"));
        }
    }

    public static async Task<IResult> VnpayIpn(
        HttpRequest request,
        IMessageBus bus,
        IUnitOfWork uow,
        ILogger<Program> logger)
    {
        logger.LogInformation("[VnpayIPN] Received IPN callback from {RemoteIp}, QueryString={QueryString}",
            request.HttpContext.Connection.RemoteIpAddress, request.QueryString);

        var gatewayParams = request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

        if (!gatewayParams.TryGetValue("vnp_TxnRef", out var txnRef) || string.IsNullOrWhiteSpace(txnRef))
        {
            return Results.Ok(new VnpayIpnResponse("99", "Invalid request"));
        }

        if (!gatewayParams.TryGetValue("vnp_Amount", out var amountRaw)
            || !long.TryParse(amountRaw, out var gatewayAmount))
        {
            return Results.Ok(new VnpayIpnResponse("99", "Invalid amount"));
        }

        var transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default);
        if (transaction is null)
        {
            return Results.Ok(new VnpayIpnResponse("01", "Order not found"));
        }

        var expectedAmount = Convert.ToInt64(decimal.Round(transaction.Amount * 100m, 0, MidpointRounding.AwayFromZero));
        if (expectedAmount != gatewayAmount)
        {
            return Results.Ok(new VnpayIpnResponse("04", "Invalid amount"));
        }

        if (transaction.Status != PaymentTransactionStatus.Pending)
        {
            return Results.Ok(new VnpayIpnResponse("02", "Order already confirmed"));
        }

        var command = new VerifyPaymentCommand
        {
            BookingId = transaction.BookingId,
            GatewayTransactionId = txnRef,
            PaymentMethod = PaymentMethod.VnPay.ToString(),
            GatewayResponseParams = gatewayParams
        };

        try
        {
            var response = await bus.InvokeAsync<VerifyPaymentResponse>(command);
            if (response.IsSuccess)
            {
                return Results.Ok(new VnpayIpnResponse("00", "Confirm Success"));
            }

            if (!string.IsNullOrWhiteSpace(response.ErrorMessage)
                && response.ErrorMessage.Contains("signature", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(new VnpayIpnResponse("97", "Invalid signature"));
            }

            // Merchant processed payment result (including fail status), so return success to stop retries.
            return Results.Ok(new VnpayIpnResponse("00", "Confirm Success"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok(new VnpayIpnResponse("97", "Invalid signature"));
        }
        catch
        {
            return Results.Ok(new VnpayIpnResponse("99", "Unknown error"));
        }
    }

    public static async Task<IResult> GetPaymentResult(
        [FromQuery] Guid? bookingId,
        [FromQuery] string txnRef,
        IUnitOfWork uow)
    {
        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return Results.BadRequest(new PaymentResultLookupResponse(
                BookingId: null,
                IsSuccess: false,
                Status: "invalid_request",
                CheckinQrCode: null,
                Booking: null,
                ErrorMessage: "txnRef is required.",
                CanRetry: false));
        }

        var transaction = await uow.PaymentTransactions.GetByGatewayTransactionIdAsync(txnRef, default);
        if (transaction is null)
        {
            return Results.NotFound(new PaymentResultLookupResponse(
                BookingId: bookingId,
                IsSuccess: false,
                Status: "not_found",
                CheckinQrCode: null,
                Booking: null,
                ErrorMessage: "Payment transaction was not found.",
                CanRetry: false));
        }

        if (bookingId.HasValue && bookingId.Value != Guid.Empty && transaction.BookingId != bookingId.Value)
        {
            return Results.NotFound(new PaymentResultLookupResponse(
                BookingId: bookingId,
                IsSuccess: false,
                Status: "not_found",
                CheckinQrCode: null,
                Booking: null,
                ErrorMessage: "Payment transaction does not belong to booking.",
                CanRetry: false));
        }

        var resolvedBookingId = transaction.BookingId;
        var booking = await uow.Bookings.LoadFullAsync(resolvedBookingId, default);
        if (booking is null)
        {
            return Results.NotFound(new PaymentResultLookupResponse(
                BookingId: resolvedBookingId,
                IsSuccess: false,
                Status: "not_found",
                CheckinQrCode: null,
                Booking: null,
                ErrorMessage: "Booking was not found.",
                CanRetry: false));
        }

        var isSuccess = transaction.Status == PaymentTransactionStatus.Success
                        && booking.Status == BookingStatus.Confirmed
                        && !string.IsNullOrWhiteSpace(booking.QrCode);

        var responseStatus = isSuccess
            ? "confirmed"
            : transaction.Status == PaymentTransactionStatus.Pending
                ? "pending_payment"
                : "payment_failed";

        return Results.Ok(new PaymentResultLookupResponse(
            BookingId: booking.Id,
            IsSuccess: isSuccess,
            Status: responseStatus,
            CheckinQrCode: booking.QrCode,
            Booking: BuildBookingDetailsResponse(booking),
            ErrorMessage: isSuccess ? null : "Payment has not been confirmed yet.",
            CanRetry: !isSuccess));
    }

    public static async Task<IResult> GetAvailableGateways(IMessageBus bus)
    {
        var gateways = await bus.InvokeAsync<IReadOnlyList<PaymentGatewayOptionDto>>(new GetAvailableGatewaysQuery());
        return Results.Ok(gateways);
    }

    private static BookingDetailsDto BuildBookingDetailsResponse(Booking booking)
    {
        return new BookingDetailsDto
        {
            BookingId = booking.Id,
            ShowTimeId = booking.ShowTimeId,
            ShowTimeInfo = new ShowTimeInfo(
                booking.ShowTime!.Screen!.Code,
                booking.ShowTime.Movie!.Name,
                booking.ShowTime.StartAt,
                booking.ShowTime.EndAt),
            OriginalAmount = booking.OriginAmount,
            DiscountAmount = booking.OriginAmount - booking.FinalAmount,
            FinalAmount = booking.FinalAmount,
            CheckinQrCode = booking.QrCode ?? string.Empty,
            CreatedAt = booking.CreatedAt,
            Status = booking.Status,
            Tickets = booking.Tickets.Select(t => new TicketInfo(t.Ticket!.SeatCode, t.Ticket.Price)).ToList(),
            TicketIds = booking.Tickets.Select(t => t.TicketId).ToList(),
            Concessions = booking.Concessions.Select(c => new ConcessionInfo(
                c.Concession!.Name,
                c.Concession.ImageUrl,
                c.Concession.Price,
                c.Quantity,
                c.Concession.Price * c.Quantity)).ToList()
        };
    }

    private static async Task<Dictionary<string, string>> ReadBodyAsStringDictionaryAsync(HttpRequest request)
    {
        if (request.Body is null)
            return [];

        try
        {
            using var document = await JsonDocument.ParseAsync(request.Body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return [];

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                result[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => string.Empty,
                    _ => property.Value.GetRawText()
                };
            }

            return result;
        }
        catch
        {
            return [];
        }
    }
}

public sealed record VnpayIpnResponse(string RspCode, string Message);
public sealed record MomoIpnResponse(int ResultCode, string Message);
public sealed record PaymentResultLookupResponse(
    Guid? BookingId,
    bool IsSuccess,
    string Status,
    string? CheckinQrCode,
    BookingDetailsDto? Booking,
    string? ErrorMessage,
    bool CanRetry);


using CinemaTicketBooking.Domain.Enums;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Represents a concession item selected during checkout with its desired quantity.
/// </summary>
public record CheckoutConcessionSelection(Guid ConcessionId, int Quantity);

/// <summary>
/// Creates booking and starts payment processing after pre-checkout validation succeeds.
/// </summary>
public class CreateBookingCommand : ICommand
{
    /// <summary>
    /// The showtime for which the booking is being created.
    /// </summary>
    public Guid ShowTimeId { get; set; }

    /// <summary>
    /// IDs of the tickets selected by the customer for this booking.
    /// </summary>
    public List<Guid> SelectedTicketIds { get; set; } = [];

    /// <summary>
    /// Session identifier for guest/anonymous customers.
    /// </summary>
    public string CustomerSessionId { get; set; } = string.Empty;

    /// <summary>
    /// Customer display name.
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Customer contact phone number.
    /// </summary>
    public string CustomerPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Customer email address for notifications and payment gateway.
    /// </summary>
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional list of concession items to add to the booking.
    /// </summary>
    public List<CheckoutConcessionSelection> Concessions { get; set; } = [];

    /// <summary>
    /// Correlation ID for tracing the booking request across services.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Payment method identifier (e.g., VnPay, Momo).
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>
    /// URL to redirect the customer after payment completes.
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Customer IP address for payment gateway fraud checks.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Optional coupon code to apply to this booking.
    /// </summary>
    public string? CouponCode { get; set; }
}

/// <summary>
/// Handles booking creation, payment gateway call, and atomic persistence.
/// </summary>
public class CreateBookingHandler(
    IUnitOfWork uow,
    ITicketLocker locker,
    IOptions<TicketLockingOptions> options,
    IPaymentServiceFactory paymentServiceFactory,
    IUserContext userContext,
    IBookingPricingService pricingService)
{
    /// <summary>
    /// Re-validates selection, creates booking, initiates payment, and persists everything atomically.
    /// 1. Load showtime and validate seats.
    /// 2. Resolve customer (authenticated, tracked-by-session, or guest).
    /// 3. Create booking entity with tickets and concessions.
    /// 4. Calculate pricing via IBookingPricingService.
    /// 5. Initiate payment and persist transaction.
    /// 6. Commit and release ticket locks.
    /// </summary>
    public async Task<CreateBookingResponse> Handle(
        CreateBookingCommand command,
        CancellationToken ct)
    {
        // 1. Load showtime and validate seat selection
        var showTime = await uow.ShowTimes.LoadFullAsync(command.ShowTimeId, ct)
            ?? throw new InvalidOperationException($"ShowTime with ID '{command.ShowTimeId}' was not found.");

        if (showTime.Status == ShowTimeStatus.Cancelled)
        {
            throw new InvalidOperationException("ShowTime is cancelled. Cannot create booking.");
        }
        var policy = await uow.SeatSelectionPolicies.GetActiveGlobalAsync(ct)
            ?? SeatSelectionPolicy.CreateDefault();

        var seatValidator = SeatSelectionValidator.CreateDefault();
        var seatValidationResult = seatValidator.Validate(
            showTime,
            policy,
            command.SelectedTicketIds,
            command.CustomerSessionId);
        if (!seatValidationResult.CanProceed)
        {
            var messages = string.Join("; ", seatValidationResult.Errors.Select(x => x.Message));
            throw new InvalidOperationException($"Seat selection cannot proceed: {messages}");
        }

        // 2. Resolve customer: authenticated → tracked-by-session → guest
        Customer? customer = null;
        if (userContext.IsAuthenticated && userContext.CustomerId.HasValue)
        {
            customer = await uow.Customers.GetByIdAsync(userContext.CustomerId.Value, ct);
            if (customer != null && !string.IsNullOrWhiteSpace(command.CustomerSessionId) && customer.SessionId != command.CustomerSessionId)
            {
                customer.SessionId = command.CustomerSessionId;
            }
        }

        customer ??= string.IsNullOrWhiteSpace(command.CustomerSessionId)
            ? null
            : await uow.Customers.GetTrackedBySessionIdAsync(command.CustomerSessionId, ct);

        customer ??= BuildGuestCustomer(command);

        // 3. Create booking and attach tickets + concessions
        var booking = Booking.Create(
            showTimeId: command.ShowTimeId,
            customerId: customer.Id == Guid.Empty ? null : customer.Id,
            customerName: command.CustomerName,
            phoneNumber: command.CustomerPhoneNumber,
            email: command.CustomerEmail,
            status: BookingStatus.Pending);
        booking.Customer = customer;

        var paymentExpiresAt = DateTimeOffset.UtcNow.Add(options.Value.PaymentHoldDuration);
        var selectedTickets = showTime.Tickets
            .Where(x => command.SelectedTicketIds.Contains(x.Id))
            .ToList();
        if (selectedTickets.Count != command.SelectedTicketIds.Count)
        {
            throw new InvalidOperationException("One or more selected tickets were not found for this showtime.");
        }

        foreach (var ticket in selectedTickets)
        {
            booking.AddTicket(ticket);
        }

        foreach (var ticket in selectedTickets)
        {
            ticket.StartPayment(booking.Id, command.CustomerSessionId, paymentExpiresAt);
            uow.Tickets.Update(ticket);
        }

        if (command.Concessions.Count > 0)
        {
            var concessionIds = command.Concessions.Select(c => c.ConcessionId).Distinct().ToList();
            var concessions = await uow.Concessions.GetByIdsAsync(concessionIds, ct);
            var concessionMap = concessions.ToDictionary(c => c.Id);

            foreach (var selectedConcession in command.Concessions)
            {
                if (!concessionMap.TryGetValue(selectedConcession.ConcessionId, out var concession))
                {
                    throw new InvalidOperationException(
                        $"Concession with ID '{selectedConcession.ConcessionId}' was not found.");
                }

                booking.AddConcession(concession, selectedConcession.Quantity);
            }
        }

        // 4. Calculate pricing via shared pricing pipeline
        var pricingResult = await pricingService.CalculateAsync(
            new PricingContext(
                Booking: booking,
                Customer: customer,
                ShowTime: showTime,
                SelectedTickets: selectedTickets,
                CouponCode: command.CouponCode,
                Mode: PricingMode.Booking),
            ct);

        booking.SetFinalAmount(pricingResult.FinalAmount);
        uow.Bookings.Add(booking);

        // 5. Initiate payment
        var method = Enum.Parse<PaymentMethod>(command.PaymentMethod, ignoreCase: true);
        var paymentService = paymentServiceFactory.GetService(method);

        var transactionId = Guid.CreateVersion7();

        var paymentResult = await paymentService.CreatePaymentAsync(new CreatePaymentRequest(
            BookingId: booking.Id,
            PaymentTransactionId: transactionId,
            Amount: booking.FinalAmount,
            OrderDescription: $"Booking {booking.Id}",
            CustomerEmail: command.CustomerEmail,
            ReturnUrl: command.ReturnUrl,
            IpAddress: command.IpAddress), ct);

        if (!paymentResult.Success)
            throw new InvalidOperationException(
                $"Payment gateway failed: {paymentResult.ErrorMessage}");

        var transaction = new PaymentTransaction
        {
            Id = transactionId,
            BookingId = booking.Id,
            Method = method,
            GatewayTransactionId = paymentResult.GatewayTransactionId,
            RedirectBehavior = paymentResult.RedirectBehavior,
            PaymentUrl = paymentResult.PaymentUrl,
            Amount = booking.FinalAmount,
            Status = PaymentTransactionStatus.Pending,
            ExpiresAt = paymentExpiresAt
        };
        uow.PaymentTransactions.Add(transaction);

        // 6. Commit and release ticket locks
        await uow.CommitAsync(ct);
        foreach (var ticket in selectedTickets)
        {
            await locker.ReleaseAsync(ticket.Id, command.CustomerSessionId, ct);
        }

        return new CreateBookingResponse(
            BookingId: booking.Id,
            PaymentExpiresAt: paymentExpiresAt,
            OriginAmount: booking.OriginAmount,
            FinalAmount: booking.FinalAmount,
            PaymentStatus: "pending_payment",
            PaymentUrl: paymentResult.PaymentUrl,
            RedirectBehavior: paymentResult.RedirectBehavior,
            PaymentTransactionId: transaction.Id,
            GatewayTransactionId: paymentResult.GatewayTransactionId,
            PromotionDiscountAmount: pricingResult.PromotionDiscountAmount,
            AppliedPromotions: pricingResult.AppliedPromotions,
            FreeItems: pricingResult.FreeItems);
    }

    /// <summary>
    /// Creates a transient guest customer from the command payload.
    /// Used when no authenticated or tracked-by-session customer is found.
    /// </summary>
    private static Customer BuildGuestCustomer(CreateBookingCommand command)
    {
        return new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = command.CustomerName,
            Email = command.CustomerEmail,
            PhoneNumber = command.CustomerPhoneNumber,
            SessionId = command.CustomerSessionId,
            IsRegistered = false
        };
    }
}

/// <summary>
/// Validates booking + payment start payload.
/// </summary>
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.ShowTimeId)
            .NotEmpty()
            .WithMessage("ShowTime ID is required.");

        RuleFor(x => x.CustomerSessionId)
            .NotEmpty()
            .WithMessage("Customer session ID is required.")
            .MaximumLength(MaxLengthConsts.SessionId);

        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .WithMessage("Customer name is required.")
            .MaximumLength(MaxLengthConsts.Name);

        RuleFor(x => x.CustomerPhoneNumber)
            .NotEmpty()
            .WithMessage("Customer phone number is required.")
            .MaximumLength(MaxLengthConsts.PhoneNumber);

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .WithMessage("Customer email is required.")
            .MaximumLength(MaxLengthConsts.Email);

        RuleFor(x => x.SelectedTicketIds)
            .NotEmpty()
            .WithMessage("Selected ticket IDs are required.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty()
            .WithMessage("Payment method is required.");

        RuleFor(x => x.ReturnUrl)
            .NotEmpty()
            .WithMessage("Return URL is required.")
            .MaximumLength(MaxLengthConsts.Url);

        RuleForEach(x => x.Concessions)
            .ChildRules(concession =>
            {
                concession.RuleFor(x => x.ConcessionId)
                    .NotEmpty()
                    .WithMessage("Concession ID is required.");
                concession.RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Concession quantity must be greater than zero.");
            });
    }
}

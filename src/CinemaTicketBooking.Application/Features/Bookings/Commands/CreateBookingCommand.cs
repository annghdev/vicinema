using CinemaTicketBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Application.Features;

public record CheckoutConcessionSelection(Guid ConcessionId, int Quantity);

/// <summary>
/// Creates booking and starts payment processing after pre-checkout validation succeeds.
/// </summary>
public class CreateBookingCommand : ICommand
{
    public Guid ShowTimeId { get; set; }
    public List<Guid> SelectedTicketIds { get; set; } = [];
    public string CustomerSessionId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhoneNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<CheckoutConcessionSelection> Concessions { get; set; } = [];
    public string CorrelationId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
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
    IEnumerable<IDiscountStrategy> discountStrategies)
{
    /// <summary>
    /// Re-validates selection, creates booking, initiates payment, and persists everything atomically.
    /// </summary>
    public async Task<CreateBookingResponse> Handle(
        CreateBookingCommand command,
        CancellationToken ct)
    {
        // 1. Load aggregate graph and policy.
        var showTime = await uow.ShowTimes.LoadFullAsync(command.ShowTimeId, ct)
            ?? throw new InvalidOperationException($"ShowTime with ID '{command.ShowTimeId}' was not found.");

        if (showTime.Status == ShowTimeStatus.Cancelled)
        {
            throw new InvalidOperationException("ShowTime is cancelled. Cannot create booking.");
        }
        var policy = await uow.SeatSelectionPolicies.GetActiveGlobalAsync(ct)
            ?? SeatSelectionPolicy.CreateDefault();

        // 2. Re-run domain validation at commit boundary.
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

        // 3. Resolve customer context used by Booking.AddTicket lock ownership check.
        // Priority: 1. Authenticated customer, 2. Guest by session id, 3. Create new guest.
        Customer? customer = null;
        if (userContext.IsAuthenticated && userContext.CustomerId.HasValue)
        {
            customer = await uow.Customers.GetByIdAsync(userContext.CustomerId.Value, ct);
            // Ensure the registered customer's current session is updated to match the checkout session.
            // This allows the Domain model (Booking.AddTicket) to validate ticket ownership.
            if (customer != null && !string.IsNullOrWhiteSpace(command.CustomerSessionId) && customer.SessionId != command.CustomerSessionId)
            {
                customer.SessionId = command.CustomerSessionId;
            }
        }

        customer ??= string.IsNullOrWhiteSpace(command.CustomerSessionId)
            ? null
            : await uow.Customers.GetTrackedBySessionIdAsync(command.CustomerSessionId, ct);

        customer ??= BuildGuestCustomer(command);

        var booking = Booking.Create(
            showTimeId: command.ShowTimeId,
            customerId: customer.Id == Guid.Empty ? null : customer.Id,
            customerName: command.CustomerName,
            phoneNumber: command.CustomerPhoneNumber,
            email: command.CustomerEmail,
            status: BookingStatus.Pending);
        booking.Customer = customer;

        // 4. Build booking items and transition all selected tickets using one shared expiry.
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

        // 5. Optional concessions and final amount calculation.
        if (command.Concessions.Count > 0)
        {
            foreach (var selectedConcession in command.Concessions)
            {
                var concession = await uow.Concessions.GetByIdAsync(selectedConcession.ConcessionId, ct);
                if (concession is null)
                {
                    throw new InvalidOperationException(
                        $"Concession with ID '{selectedConcession.ConcessionId}' was not found.");
                }

                booking.AddConcession(concession, selectedConcession.Quantity);
            }
        }

        // 5b. Calculate loyalty discount server-side using all registered strategies.
        decimal discountAmount = 0m;
        decimal couponDiscountAmount = 0m;
        if (customer.IsRegistered)
        {
            var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);
            var aggregator = new DiscountStrategyComposite(discountStrategies);
            discountAmount = aggregator.CalculateTotalDiscount(booking, customer, activeTiers);
        }

        // 5c. Apply coupon if provided.
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            couponDiscountAmount = await ApplyCouponAndGetDiscountAsync(command.CouponCode, customer, booking, ct);
        }

        booking.UpdateFinalAmount(discountAmount, couponDiscountAmount);
        uow.Bookings.Add(booking);

        // 6. Initiate payment via selected gateway (before commit).
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

        // 7. Build PaymentTransaction record.
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

        // 8. Atomic commit: Booking + Tickets + PaymentTransaction in one transaction.
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
            GatewayTransactionId: paymentResult.GatewayTransactionId);
    }

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

    private async Task<decimal> ApplyCouponAndGetDiscountAsync(
        string couponCode,
        Customer? customer,
        Booking booking,
        CancellationToken ct)
    {
        var code = couponCode.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;
        CustomerCoupon? customerCoupon = null;
        CouponTemplate? template = null;

        // Try personal coupon first
        if (customer?.IsRegistered == true)
        {
            customerCoupon = await uow.CustomerCoupons.GetByCustomerAndCodeAsync(customer.Id, code, ct);
        }

        if (customerCoupon is not null)
        {
            customerCoupon.ValidateAvailable(now);
        }
        else
        {
            template = await uow.CouponTemplates.GetByCodeAsync(code, ct);
            if (template is null)
                throw new InvalidOperationException("Mã giảm giá không hợp lệ.");

            template.ValidateAvailable(now);
        }

        var discountType = customerCoupon?.DiscountType ?? template!.DiscountType;
        var discountValue = customerCoupon?.DiscountValue ?? template!.DiscountValue;
        var maxDiscountAmount = customerCoupon?.MaxDiscountAmount ?? template!.MaxDiscountAmount;
        var scope = customerCoupon?.Scope ?? template!.Scope;

        decimal qualifyingAmount = scope switch
        {
            DiscountScope.All => booking.OriginAmount,
            DiscountScope.Tickets => booking.Tickets.Sum(t => t.Ticket?.Price ?? 0m),
            DiscountScope.Concessions => booking.Concessions.Sum(c => (c.Concession?.Price ?? 0m) * c.Quantity),
            _ => booking.OriginAmount
        };

        decimal discount;
        if (discountType == DiscountType.Fixed)
        {
            discount = Math.Min(discountValue, qualifyingAmount);
        }
        else
        {
            discount = qualifyingAmount * discountValue / 100m;
            if (maxDiscountAmount.HasValue)
                discount = Math.Min(discount, maxDiscountAmount.Value);
        }

        discount = Math.Round(discount, 2);

        // Apply coupon to booking (domain logic + raise event)
        booking.ApplyCoupon(code, discount);

        // Persist coupon usage
        if (customerCoupon is not null)
        {
            customerCoupon.MarkUsed(now);
            uow.CustomerCoupons.Update(customerCoupon);
        }

        // If it's a public template, increment global usage
        if (template is not null)
        {
            template.IncrementUsage();
            uow.CouponTemplates.Update(template);

            // Create a CustomerCoupon record to track per-user usage
            var usageRecord = CustomerCoupon.RedeemPublic(customer?.Id ?? Guid.Empty, template, now);

            // Mark as used immediately to prevent re-use by the same customer
            usageRecord.MarkUsed(now);

            uow.CustomerCoupons.Add(usageRecord);
        }

        return discount;
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

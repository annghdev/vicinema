using CinemaTicketBooking.Domain.Services;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Previews the final pricing breakdown (tickets + concessions + loyalty discount)
/// using the same domain services as the real booking creation.
/// Does NOT mutate any state.
/// </summary>
public class PreviewPricingQuery : IQuery<PreviewPricingResponse>
{
    public Guid ShowTimeId { get; set; }
    public List<Guid> SelectedTicketIds { get; set; } = [];
    public string CustomerSessionId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhoneNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<CheckoutConcessionSelection> Concessions { get; set; } = [];
    public string CorrelationId { get; set; } = string.Empty;
}

/// <summary>
/// Handler that reuses the same discount-composition logic as CreateBookingHandler
/// but returns a detailed price breakdown without persisting anything.
/// </summary>
public class PreviewPricingHandler(
    IUnitOfWork uow,
    IEnumerable<IDiscountStrategy> discountStrategies,
    ILoyaltyDiscountService loyaltyDiscountService)
{
    public async Task<PreviewPricingResponse> Handle(
        PreviewPricingQuery query,
        CancellationToken ct)
    {
        // 1. Load showtime with tickets.
        var showTime = await uow.ShowTimes.LoadFullAsync(query.ShowTimeId, ct)
            ?? throw new InvalidOperationException($"ShowTime with ID '{query.ShowTimeId}' was not found.");

        // 2. Resolve selected tickets.
        var selectedTickets = showTime.Tickets
            .Where(x => query.SelectedTicketIds.Contains(x.Id))
            .ToList();

        decimal ticketOrigin = selectedTickets.Sum(t => t.Price);

        // 3. Resolve selected concessions and compute concession origin.
        var resolvedConcessions = new List<(Concession Concession, int Quantity)>();
        decimal concessionOrigin = 0m;

        foreach (var sel in query.Concessions)
        {
            var concession = await uow.Concessions.GetByIdAsync(sel.ConcessionId, ct);
            if (concession is not null && concession.IsAvailable)
            {
                concessionOrigin += concession.Price * sel.Quantity;
                resolvedConcessions.Add((concession, sel.Quantity));
            }
        }

        decimal originAmount = ticketOrigin + concessionOrigin;

        // 4. Find customer (same resolution as CreateBookingHandler).
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(query.CustomerSessionId))
        {
            customer = await uow.Customers.GetTrackedBySessionIdAsync(query.CustomerSessionId, ct);
        }

        decimal ticketDiscount = 0m;
        decimal concessionDiscount = 0m;
        string? loyaltyTierName = null;
        string? loyaltyTierDescription = null;
        decimal? ticketDiscountPercent = null;
        decimal? concessionDiscountPercent = null;

        // 5. Run discount computation when customer is registered.
        if (customer?.IsRegistered == true)
        {
            var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);

            // Build a lightweight in-memory Booking so the domain services can
            // calculate discount without persisting anything.
            var tempBooking = BuildPreviewBooking(
                query, selectedTickets, resolvedConcessions, originAmount);

            var aggregator = new DiscountStrategyComposite(discountStrategies);
            aggregator.CalculateTotalDiscount(
                tempBooking, customer, activeTiers);

            // Extract per-category discount details for display.
            var tierConfig = activeTiers.FirstOrDefault(t => t.Tier == customer.LoyaltyTier);
            if (tierConfig is not null)
            {
                var result = loyaltyDiscountService.CalculateDiscount(
                    tempBooking, customer, tierConfig);
                ticketDiscount = result.TicketDiscount;
                concessionDiscount = result.ConcessionDiscount;
                loyaltyTierName = tierConfig.Name;
                loyaltyTierDescription = tierConfig.Description;
                ticketDiscountPercent = tierConfig.TicketDiscountPercent;
                concessionDiscountPercent = tierConfig.ConcessionDiscountPercent;
            }
        }

        return new PreviewPricingResponse(
            OriginAmount: originAmount,
            TicketDiscount: ticketDiscount,
            ConcessionDiscount: concessionDiscount,
            TotalDiscount: ticketDiscount + concessionDiscount,
            FinalAmount: Math.Max(0, originAmount - ticketDiscount - concessionDiscount),
            IsRegisteredCustomer: customer?.IsRegistered ?? false,
            LoyaltyTierName: loyaltyTierName,
            LoyaltyTierDescription: loyaltyTierDescription,
            TicketDiscountPercent: ticketDiscountPercent,
            ConcessionDiscountPercent: concessionDiscountPercent);
    }

    private static Booking BuildPreviewBooking(
        PreviewPricingQuery query,
        List<Ticket> selectedTickets,
        List<(Concession Concession, int Quantity)> resolvedConcessions,
        decimal originAmount)
    {
        var booking = new Booking
        {
            ShowTimeId = query.ShowTimeId,
            CustomerName = query.CustomerName,
            PhoneNumber = query.CustomerPhoneNumber,
            Email = query.CustomerEmail,
            Status = BookingStatus.Pending,
            OriginAmount = originAmount,
            FinalAmount = originAmount,
            Tickets = selectedTickets
                .Select(t => new BookingTicket
                {
                    Id = Guid.CreateVersion7(),
                    TicketId = t.Id,
                    Ticket = t
                })
                .ToList(),
            Concessions = resolvedConcessions
                .Select(rc => new BookingConcession
                {
                    Id = Guid.CreateVersion7(),
                    ConcessionId = rc.Concession.Id,
                    Concession = rc.Concession,
                    Quantity = rc.Quantity
                })
                .ToList()
        };

        return booking;
    }
}

/// <summary>
/// Validates the preview pricing request.
/// </summary>
public class PreviewPricingValidator : AbstractValidator<PreviewPricingQuery>
{
    public PreviewPricingValidator()
    {
        RuleFor(x => x.ShowTimeId)
            .NotEmpty()
            .WithMessage("ShowTime ID is required.");

        RuleFor(x => x.CustomerSessionId)
            .NotEmpty()
            .WithMessage("Customer session ID is required.")
            .MaximumLength(MaxLengthConsts.SessionId);

        RuleFor(x => x.SelectedTicketIds)
            .NotEmpty()
            .WithMessage("Selected ticket IDs are required.");
    }
}
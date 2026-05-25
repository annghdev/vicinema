using CinemaTicketBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using CinemaTicketBooking.Application.Features.Promotions;

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

    /// <summary>
    /// Optional coupon code to apply in the preview.
    /// </summary>
    public string? CouponCode { get; set; }
}

/// <summary>
/// Handler that delegates the full pricing pipeline to IBookingPricingService.
/// Does NOT mutate any state — uses PricingMode.Preview.
/// </summary>
public class PreviewPricingHandler(
    IUnitOfWork uow,
    IBookingPricingService pricingService)
{
    public async Task<PreviewPricingResponse> Handle(
        PreviewPricingQuery query,
        CancellationToken ct)
    {
        var showTime = await uow.ShowTimes.LoadFullAsync(query.ShowTimeId, ct)
            ?? throw new InvalidOperationException($"ShowTime with ID '{query.ShowTimeId}' was not found.");

        var selectedTickets = showTime.Tickets
            .Where(x => query.SelectedTicketIds.Contains(x.Id))
            .ToList();

        decimal ticketOrigin = selectedTickets.Sum(t => t.Price);

        var resolvedConcessions = new List<(Concession Concession, int Quantity)>();
        decimal concessionOrigin = 0m;

        if (query.Concessions.Count > 0)
        {
            var concessionIds = query.Concessions.Select(c => c.ConcessionId).Distinct().ToList();
            var concessions = await uow.Concessions.GetByIdsAsync(concessionIds, ct);
            var concessionMap = concessions.ToDictionary(c => c.Id);

            foreach (var sel in query.Concessions)
            {
                if (concessionMap.TryGetValue(sel.ConcessionId, out var concession) && concession.IsAvailable)
                {
                    concessionOrigin += concession.Price * sel.Quantity;
                    resolvedConcessions.Add((concession, sel.Quantity));
                }
            }
        }

        decimal originAmount = ticketOrigin + concessionOrigin;

        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(query.CustomerSessionId))
        {
            customer = await uow.Customers.GetTrackedBySessionIdAsync(query.CustomerSessionId, ct);
        }

        var tempBooking = BuildPreviewBooking(
            query, selectedTickets, resolvedConcessions, originAmount);

        var pricingResult = await pricingService.CalculateAsync(
            new PricingContext(
                Booking: tempBooking,
                Customer: customer,
                ShowTime: showTime,
                SelectedTickets: selectedTickets,
                CouponCode: query.CouponCode,
                Mode: PricingMode.Preview),
            ct);

        return new PreviewPricingResponse(
            OriginAmount: originAmount,
            TicketDiscount: pricingResult.TicketDiscount,
            ConcessionDiscount: pricingResult.ConcessionDiscount,
            TotalDiscount: pricingResult.TotalDiscount,
            FinalAmount: pricingResult.FinalAmount,
            IsRegisteredCustomer: customer?.IsRegistered ?? false,
            LoyaltyTierName: pricingResult.LoyaltyTierName,
            LoyaltyTierDescription: pricingResult.LoyaltyTierDescription,
            TicketDiscountPercent: pricingResult.TicketDiscountPercent,
            ConcessionDiscountPercent: pricingResult.ConcessionDiscountPercent,
            CouponDiscountAmount: pricingResult.CouponDiscountAmount,
            CouponCode: pricingResult.CouponCode,
            CouponDescription: pricingResult.CouponDescription,
            PromotionDiscountAmount: pricingResult.PromotionDiscountAmount,
            AppliedPromotions: pricingResult.AppliedPromotions,
            FreeItems: pricingResult.FreeItems);
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
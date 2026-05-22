namespace CinemaTicketBooking.Domain.Services;

/// <summary>
/// Default implementation of <see cref="ILoyaltyDiscountService"/>.
/// Computes tier-based discounts and point accumulation.
/// </summary>
public class LoyaltyDiscountService : ILoyaltyDiscountService
{
    /// <inheritdoc />
    public LoyaltyDiscountResult CalculateDiscount(
        Booking booking,
        Customer customer,
        LoyaltyTierConfiguration tierConfig)
    {
        // 1. Calculate ticket discount: sum(ticket.Price) * ticketDiscountPercent / 100
        var ticketTotal = booking.Tickets.Sum(bt => bt.Ticket?.Price ?? 0m);
        var ticketDiscount = Math.Round(ticketTotal * tierConfig.TicketDiscountPercent / 100m, 2);

        // 2. Calculate concession discount: sum(concession.Price * qty) * concessionDiscountPercent / 100
        var concessionTotal = booking.Concessions.Sum(bc => (bc.Concession?.Price ?? 0m) * bc.Quantity);
        var concessionDiscount = Math.Round(concessionTotal * tierConfig.ConcessionDiscountPercent / 100m, 2);

        return new LoyaltyDiscountResult(ticketDiscount, concessionDiscount);
    }

    /// <inheritdoc />
    public int CalculatePoints(decimal finalAmount)
    {
        // 1000 VND = 1 point, rounded down
        return (int)(finalAmount / 1000m);
    }

    /// <inheritdoc />
    public LoyaltyTier DetermineTier(int accumulatedPoints, IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
    {
        // 1. Sort tiers by tier level ascending
        var sorted = activeTiers
            .OrderBy(t => t.Tier)
            .ToList();

        // 2. Find the highest tier whose range contains accumulatedPoints
        LoyaltyTier result = LoyaltyTier.Bronze;
        foreach (var tier in sorted)
        {
            if (accumulatedPoints >= tier.MinPoints &&
                (!tier.MaxPoints.HasValue || accumulatedPoints <= tier.MaxPoints.Value))
            {
                result = tier.Tier;
                break;
            }
        }

        return result;
    }
}

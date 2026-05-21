namespace CinemaTicketBooking.Domain.Services;

/// <summary>
/// Domain service for loyalty discount calculation and point determination.
/// </summary>
public interface ILoyaltyDiscountService
{
    /// <summary>
    /// Calculates tier-based discount for a booking, returning separate amounts for tickets and concessions.
    /// </summary>
    LoyaltyDiscountResult CalculateDiscount(
        Booking booking,
        Customer customer,
        LoyaltyTierConfiguration tierConfig);

    /// <summary>
    /// Calculates loyalty points from a final booking amount (1000 VND = 1 point, rounded down).
    /// </summary>
    int CalculatePoints(decimal finalAmount);

    /// <summary>
    /// Determines the loyalty tier for a given accumulated points total
    /// based on the active tier configuration list.
    /// </summary>
    LoyaltyTier DetermineTier(int accumulatedPoints, IReadOnlyList<LoyaltyTierConfiguration> activeTiers);
}

/// <summary>
/// Result of a loyalty discount calculation, separating ticket and concession discounts.
/// </summary>
public record LoyaltyDiscountResult(decimal TicketDiscount, decimal ConcessionDiscount);

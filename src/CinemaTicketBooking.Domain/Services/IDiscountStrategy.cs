namespace CinemaTicketBooking.Domain.Services;

/// <summary>
/// Strategy for computing discounts on a booking.
/// Allows multiple discount sources (loyalty tier, coupons, promotions) to compose.
/// </summary>
public interface IDiscountStrategy
{
    /// <summary>
    /// Execution priority — lower runs first (0 = highest priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Calculates discount for the booking. Returns null if this strategy does not apply.
    /// </summary>
    LoyaltyDiscountResult? Calculate(
        Booking booking,
        Customer customer,
        IReadOnlyList<LoyaltyTierConfiguration> activeTiers);
}

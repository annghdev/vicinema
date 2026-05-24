using CinemaTicketBooking.Domain.Services;

namespace CinemaTicketBooking.Domain;

/// <summary>
/// Applies coupon discount to a booking.
/// Priority 10 — runs after the loyalty tier strategy (priority 0).
///
/// The coupon must be resolved and the discount pre-computed by the
/// calling handler. This strategy reads <see cref="Booking.CouponDiscountAmount"/>
/// and returns it as the combined result.
///
/// If the booking has no coupon (<see cref="Booking.CouponCode"/> is null/empty),
/// the strategy returns null (no-op).
/// </summary>
public class CouponDiscountStrategy : IDiscountStrategy
{
    public int Priority => 10;

    /// <inheritdoc />
    public LoyaltyDiscountResult? Calculate(
        Booking booking,
        Customer customer,
        IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
    {
        if (string.IsNullOrWhiteSpace(booking.CouponCode))
            return null;

        // CouponDiscountAmount is pre-computed by the handler.
        // For display, we allocate it all to TicketDiscount (the caller uses
        // the combined TotalDiscount from DiscountStrategyComposite).
        return new LoyaltyDiscountResult(
            booking.CouponDiscountAmount,
            0m);
    }
}
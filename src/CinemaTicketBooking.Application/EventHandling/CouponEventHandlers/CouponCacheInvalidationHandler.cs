using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Invalidates coupon cache entries when coupon-related events occur.
/// </summary>
public class CouponCacheInvalidationHandler(ICacheService cacheService)
{
    /// <summary>
    /// Invalidates customer coupon cache when a coupon is issued.
    /// </summary>
    public async Task Handle(CouponIssued @event, CancellationToken ct)
    {
        await cacheService.RemoveAsync(
            Features.Coupons.CouponCacheKeys.CustomerCoupons(@event.CustomerId), ct);
    }

    /// <summary>
    /// Invalidates customer coupon cache when a coupon is applied (used).
    /// </summary>
    public async Task Handle(CouponApplied @event, CancellationToken ct)
    {
        if (@event.CustomerId.HasValue)
        {
            await cacheService.RemoveAsync(
                Features.Coupons.CouponCacheKeys.CustomerCoupons(@event.CustomerId.Value), ct);
        }
    }
}
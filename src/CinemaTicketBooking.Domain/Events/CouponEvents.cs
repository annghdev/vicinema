namespace CinemaTicketBooking.Domain;

/// <summary>
/// Raised when a coupon is issued to a customer (e.g., loyalty upgrade reward, admin grant).
/// Side effects: send notification email, invalidate coupon cache.
/// </summary>
public record CouponIssued(
    Guid CustomerId,
    Guid CustomerCouponId,
    string CouponCode,
    DiscountType DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    DiscountScope Scope,
    DateTimeOffset ExpiresAt) : BaseDomainEvent;

/// <summary>
/// Raised when a coupon is applied to a booking.
/// Side effects: update coupon usage count, invalidate coupon cache.
/// </summary>
public record CouponApplied(
    Guid BookingId,
    Guid? CustomerId,
    Guid? CustomerCouponId,
    string CouponCode,
    decimal DiscountAmount) : BaseDomainEvent;
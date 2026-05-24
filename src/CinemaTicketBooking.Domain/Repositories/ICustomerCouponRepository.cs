namespace CinemaTicketBooking.Domain.Repositories;

/// <summary>
/// Repository for <see cref="CustomerCoupon"/>.
/// </summary>
public interface ICustomerCouponRepository : IRepository<CustomerCoupon>
{
    /// <summary>
    /// Returns all coupons for a given customer (personal + redeemed public).
    /// </summary>
    Task<IReadOnlyList<CustomerCoupon>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);

    /// <summary>
    /// Finds a specific customer coupon by customer ID and coupon code.
    /// </summary>
    Task<CustomerCoupon?> GetByCustomerAndCodeAsync(Guid customerId, string couponCode, CancellationToken ct = default);

    /// <summary>
    /// Returns only available (not expired, not used up) coupons for a customer.
    /// </summary>
    Task<IReadOnlyList<CustomerCoupon>> GetAvailableByCustomerAsync(Guid customerId, DateTimeOffset now, CancellationToken ct = default);

    /// <summary>
    /// Returns the usage count of a specific coupon code by a customer.
    /// Used to enforce per-user usage limits on public templates.
    /// </summary>
    Task<int> GetUsageCountByCustomerAndCodeAsync(Guid customerId, string couponCode, CancellationToken ct = default);
}
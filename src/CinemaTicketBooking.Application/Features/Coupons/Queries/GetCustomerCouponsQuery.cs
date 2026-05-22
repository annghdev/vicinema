using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Returns all coupons (personal + redeemed public) for the authenticated customer.
/// </summary>
public class GetCustomerCouponsQuery : IQuery<IReadOnlyList<CustomerCouponDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
}

public class GetCustomerCouponsHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task<IReadOnlyList<CustomerCouponDto>> Handle(GetCustomerCouponsQuery query, CancellationToken ct)
    {
        var cacheKey = CouponCacheKeys.CustomerCoupons(query.CustomerId);
        var cached = await cache.GetAsync<IReadOnlyList<CustomerCouponDto>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var coupons = await uow.CustomerCoupons.GetByCustomerIdAsync(query.CustomerId, ct);
        var result = coupons
            .Select(c => new CustomerCouponDto(
                Id: c.Id,
                CustomerId: c.CustomerId,
                CouponCode: c.CouponCode,
                DiscountType: c.DiscountType.ToString(),
                DiscountValue: c.DiscountValue,
                MaxDiscountAmount: c.MaxDiscountAmount,
                Scope: c.Scope.ToString(),
                UsageCount: c.UsageCount,
                MaxUsage: c.MaxUsage,
                IsUsed: c.IsUsed,
                IssuedAt: c.IssuedAt,
                UsedAt: c.UsedAt,
                ExpiresAt: c.ExpiresAt))
            .ToList() as IReadOnlyList<CustomerCouponDto>;

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}
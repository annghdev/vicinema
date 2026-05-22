using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Returns all coupon templates (admin).
/// </summary>
public class GetAllCouponTemplatesQuery : IQuery<IReadOnlyList<CouponTemplateDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
}

public class GetAllCouponTemplatesHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task<IReadOnlyList<CouponTemplateDto>> Handle(GetAllCouponTemplatesQuery query, CancellationToken ct)
    {
        var cached = await cache.GetAsync<IReadOnlyList<CouponTemplateDto>>(CouponCacheKeys.AllTemplatesKey, ct);
        if (cached is not null)
            return cached;

        var templates = await uow.CouponTemplates.GetAllOrderedAsync(ct);
        var result = templates
            .Select(t => new CouponTemplateDto(
                Id: t.Id,
                Code: t.Code,
                Type: t.Type.ToString(),
                DiscountType: t.DiscountType.ToString(),
                DiscountValue: t.DiscountValue,
                MaxDiscountAmount: t.MaxDiscountAmount,
                Scope: t.Scope.ToString(),
                MaxUsageCount: t.MaxUsageCount,
                MaxUsagePerUser: t.MaxUsagePerUser,
                DurationDays: t.DurationDays,
                TotalUsedCount: t.TotalUsedCount,
                IsActive: t.IsActive,
                Description: t.Description,
                CreatedAt: t.CreatedAt))
            .ToList() as IReadOnlyList<CouponTemplateDto>;

        await cache.SetAsync(CouponCacheKeys.AllTemplatesKey, result, TimeSpan.FromMinutes(10), ct);
        return result;
    }
}
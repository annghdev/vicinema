using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Returns all active public coupon templates (for KOL/shared codes).
/// </summary>
public class GetPublicCouponTemplatesQuery : IQuery<IReadOnlyList<CouponTemplateDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
}

public class GetPublicCouponTemplatesHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task<IReadOnlyList<CouponTemplateDto>> Handle(GetPublicCouponTemplatesQuery query, CancellationToken ct)
    {
        var cached = await cache.GetAsync<IReadOnlyList<CouponTemplateDto>>(CouponCacheKeys.PublicTemplatesKey, ct);
        if (cached is not null)
            return cached;

        var templates = await uow.CouponTemplates.GetActivePublicTemplatesAsync(ct);
        var result = templates
            .Select(MapDto)
            .ToList() as IReadOnlyList<CouponTemplateDto>;

        await cache.SetAsync(CouponCacheKeys.PublicTemplatesKey, result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    private static CouponTemplateDto MapDto(CouponTemplate t) => new(
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
        CreatedAt: t.CreatedAt);
}
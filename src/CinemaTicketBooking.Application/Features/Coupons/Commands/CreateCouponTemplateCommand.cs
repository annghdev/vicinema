using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Creates a new coupon template (admin).
/// </summary>
public class CreateCouponTemplateCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Public";
    public string DiscountType { get; set; } = "Fixed";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public string Scope { get; set; } = "All";
    public int MaxUsageCount { get; set; }
    public int MaxUsagePerUser { get; set; } = 1;
    public int DurationDays { get; set; } = 30;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CreateCouponTemplateHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task Handle(CreateCouponTemplateCommand command, CancellationToken ct)
    {
        var couponType = Enum.Parse<CouponType>(command.Type, ignoreCase: true);
        var discountType = Enum.Parse<DiscountType>(command.DiscountType, ignoreCase: true);
        var scope = Enum.Parse<DiscountScope>(command.Scope, ignoreCase: true);

        // Check code uniqueness
        var existing = await uow.CouponTemplates.GetByCodeAsync(command.Code.Trim().ToUpperInvariant(), ct);
        if (existing is not null)
            throw new InvalidOperationException($"Mã giảm giá '{command.Code}' đã tồn tại.");

        var template = CouponTemplate.Create(
            code: command.Code,
            type: couponType,
            discountType: discountType,
            discountValue: command.DiscountValue,
            maxDiscountAmount: command.MaxDiscountAmount,
            scope: scope,
            maxUsageCount: couponType == CouponType.Public ? command.MaxUsageCount : 0,
            maxUsagePerUser: couponType == CouponType.Public ? command.MaxUsagePerUser : 1,
            durationDays: command.DurationDays,
            description: command.Description,
            isActive: command.IsActive);

        uow.CouponTemplates.Add(template);
        await uow.CommitAsync(ct);

        // Invalidate caches
        await cache.RemoveAsync(CouponCacheKeys.AllTemplatesKey, ct);
        await cache.RemoveAsync(CouponCacheKeys.PublicTemplatesKey, ct);
        await cache.RemoveAsync(CouponCacheKeys.TemplateByCode(template.Code), ct);
    }
}
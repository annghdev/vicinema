using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Toggles a coupon template's active status (admin).
/// </summary>
public class ToggleCouponTemplateCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
}

public class ToggleCouponTemplateHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task Handle(ToggleCouponTemplateCommand command, CancellationToken ct)
    {
        var template = await uow.CouponTemplates.GetByIdAsync(command.TemplateId, ct)
            ?? throw new InvalidOperationException("Không tìm thấy mã giảm giá.");

        if (template.IsActive)
            template.Deactivate();
        else
            template.Activate();

        uow.CouponTemplates.Update(template);
        await uow.CommitAsync(ct);

        // Invalidate caches
        await cache.RemoveAsync(CouponCacheKeys.AllTemplatesKey, ct);
        await cache.RemoveAsync(CouponCacheKeys.PublicTemplatesKey, ct);
        await cache.RemoveAsync(CouponCacheKeys.TemplateByCode(template.Code), ct);
    }
}
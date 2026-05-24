using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain.Events;

namespace CinemaTicketBooking.Application.Messaging;

public class PromotionCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(PromotionCreated @event, CancellationToken ct) => await InvalidateListAsync(ct);
    public async Task Handle(PromotionUpdated @event, CancellationToken ct) => await InvalidateAllAsync(@event.PromotionProgramId, ct);
    public async Task Handle(PromotionDeleted @event, CancellationToken ct) => await InvalidateAllAsync(@event.PromotionProgramId, ct);
    public async Task Handle(PromotionActivated @event, CancellationToken ct) => await InvalidateAllAsync(@event.PromotionProgramId, ct);
    public async Task Handle(PromotionDeactivated @event, CancellationToken ct) => await InvalidateAllAsync(@event.PromotionProgramId, ct);

    private async Task InvalidateListAsync(CancellationToken ct)
    {
        await cacheService.RemoveByPrefix(Features.Promotions.PromotionCacheKeys.ListPrefix, ct);
        await cacheService.RemoveAsync(Features.Promotions.PromotionCacheKeys.ActiveKey, ct);
    }

    private async Task InvalidateAllAsync(Guid promotionId, CancellationToken ct)
    {
        await cacheService.RemoveByPrefix(Features.Promotions.PromotionCacheKeys.ListPrefix, ct);
        await cacheService.RemoveAsync(Features.Promotions.PromotionCacheKeys.ActiveKey, ct);
        await cacheService.RemoveAsync(Features.Promotions.PromotionCacheKeys.Detail(promotionId), ct);
    }
}

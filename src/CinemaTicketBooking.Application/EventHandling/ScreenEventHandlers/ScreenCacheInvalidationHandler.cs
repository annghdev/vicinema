using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class ScreenCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(ScreenCreated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenBasicInfoUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenActivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenDeactivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenSeatActivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenSeatDeactivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);
    public async Task Handle(ScreenSeatsGenerated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ScreenId, ct);

    private async Task InvalidateCacheAsync(Guid screenId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(ScreenCacheKeys.GetScreenById(screenId), ct);
        await cacheService.RemoveByPrefix(ScreenCacheKeys.ListPrefix, ct);
    }
}

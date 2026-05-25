using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class ConcessionCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(ConcessionCreated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ConcessionId, ct);
    public async Task Handle(ConcessionBasicInfoUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ConcessionId, ct);
    public async Task Handle(ConcessionDeleted @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ConcessionId, ct);
    public async Task Handle(ConcessionMarkedAvailable @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ConcessionId, ct);
    public async Task Handle(ConcessionMarkedUnavailable @event, CancellationToken ct) => await InvalidateCacheAsync(@event.ConcessionId, ct);

    private async Task InvalidateCacheAsync(Guid concessionId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(ConcessionCacheKeys.GetConcessionById(concessionId), ct);
        await cacheService.RemoveByPrefix(ConcessionCacheKeys.ListPrefix, ct);
    }
}

using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class ShowTimeCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(ShowTimeCreated @event, CancellationToken ct) => await InvalidateCacheAsync(ct);
    public async Task Handle(ShowTimeCancelled @event, CancellationToken ct) => await InvalidateCacheAsync(ct);
    public async Task Handle(ShowTimeStarted @event, CancellationToken ct) => await InvalidateCacheAsync(ct);
    public async Task Handle(ShowTimeCompleted @event, CancellationToken ct) => await InvalidateCacheAsync(ct);

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        // Only list caches are maintained for ShowTimes, so clear by prefix
        await cacheService.RemoveByPrefix(ShowTimeCacheKeys.ListPrefix, ct);
    }
}

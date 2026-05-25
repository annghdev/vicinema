using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class SlideCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(SlideCreated @event, CancellationToken ct) => await InvalidateCacheAsync(ct);
    public async Task Handle(SlideUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(ct);
    public async Task Handle(SlideDeleted @event, CancellationToken ct) => await InvalidateCacheAsync(ct);

    private async Task InvalidateCacheAsync(CancellationToken ct)
    {
        // Slides are simple, just clear by prefix
        await cacheService.RemoveByPrefix(SlideCacheKeys.ListPrefix, ct);
    }
}

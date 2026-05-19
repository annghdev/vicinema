using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class CinemaCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(CinemaCreated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.CinemaId, ct);
    public async Task Handle(CinemaBasicInfoUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.CinemaId, ct);
    public async Task Handle(CinemaDeleted @event, CancellationToken ct) => await InvalidateCacheAsync(@event.CinemaId, ct);
    public async Task Handle(CinemaActivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.CinemaId, ct);
    public async Task Handle(CinemaDeactivated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.CinemaId, ct);

    private async Task InvalidateCacheAsync(Guid cinemaId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(CinemaCacheKeys.GetCinemaById(cinemaId), ct);
        await cacheService.RemoveByPrefix(CinemaCacheKeys.ListPrefix, ct);
    }
}

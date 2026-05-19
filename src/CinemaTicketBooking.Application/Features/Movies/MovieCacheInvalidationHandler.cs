using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class MovieCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(MovieCreated @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    public async Task Handle(MovieBasicInfoUpdated @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    public async Task Handle(MovieDeleted @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    public async Task Handle(MoviePromotedToNowShowing @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    public async Task Handle(MovieWithdrawnAsNoShowWhileUpcoming @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    public async Task Handle(MovieRunClosedAsNoShow @event, CancellationToken ct)
    {
        await InvalidateCacheAsync(@event.MovieId, ct);
    }

    private async Task InvalidateCacheAsync(Guid movieId, CancellationToken ct)
    {
        // 1. Invalidate specific movie cache
        await cacheService.RemoveAsync(MovieCacheKeys.GetMovieById(movieId), ct);
        
        // 2. Invalidate all movie list/paged/dropdown caches
        await cacheService.RemoveByPrefix(MovieCacheKeys.ListPrefix, ct);
    }
}

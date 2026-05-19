using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all movies.
/// </summary>
public class GetMoviesQuery : ICachableQuery<IReadOnlyList<MovieDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => MovieCacheKeys.GetMovies();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for all movies.
/// </summary>
public class GetMoviesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all movies mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<MovieDto>> Handle(GetMoviesQuery query, CancellationToken ct)
    {
        var movies = await uow.Movies
            .GetQueryFilter()
            .Select(x => new MovieDto(
                x.Id,
                x.Name,
                x.Description,
                x.ThumbnailUrl,
                x.Studio,
                x.Director,
                x.OfficialTrailerUrl,
                x.Duration,
                x.Genre,
                x.Status,
                x.TargetReach,
                x.CreatedAt))
            .ToListAsync(ct);

        return movies;
    }
}

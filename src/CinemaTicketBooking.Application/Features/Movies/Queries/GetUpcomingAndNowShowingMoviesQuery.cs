using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets movies that are upcoming or currently showing.
/// </summary>
public class GetUpcomingAndNowShowingMoviesQuery : ICachableQuery<IReadOnlyList<MovieDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => MovieCacheKeys.GetUpcomingAndNowShowing();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for upcoming and now-showing movies.
/// </summary>
public class GetUpcomingAndNowShowingMoviesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns movies in Ongoing (upcoming) and NowShowing statuses.
    /// </summary>
    public async Task<IReadOnlyList<MovieDto>> Handle(GetUpcomingAndNowShowingMoviesQuery query, CancellationToken ct)
    {
        var movies = await uow.Movies
            .GetQueryFilter()
            .Where(movie => movie.Status == MovieStatus.Upcoming || movie.Status == MovieStatus.NowShowing)
            .OrderBy(movie => movie.Status == MovieStatus.NowShowing ? 0 : 1)
            .ThenBy(movie => movie.Name)
            .Select(movie => new MovieDto(
                movie.Id,
                movie.Name,
                movie.Description,
                movie.ThumbnailUrl,
                movie.Studio,
                movie.Director,
                movie.OfficialTrailerUrl,
                movie.Duration,
                movie.Genre,
                movie.Status,
                movie.TargetReach,
                movie.CreatedAt))
            .ToListAsync(ct);

        return movies;
    }
}

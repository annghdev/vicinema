using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets dropdown data for movies that are upcoming or currently showing.
/// </summary>
public class GetUpcomingAndNowShowingMovieDropdownQuery : ICachableQuery<IReadOnlyList<MovieDropdownDto>>
{
    public string? SearchTerm { get; set; }
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => MovieCacheKeys.GetUpcomingAndNowShowingDropdown(SearchTerm, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles dropdown query for upcoming and now-showing movies.
/// </summary>
public class GetUpcomingAndNowShowingMovieDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items in Ongoing and NowShowing statuses.
    /// </summary>
    public async Task<IReadOnlyList<MovieDropdownDto>> Handle(GetUpcomingAndNowShowingMovieDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Movies
            .GetQueryFilter()
            .Where(movie => movie.Status == MovieStatus.Upcoming || movie.Status == MovieStatus.NowShowing);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(movie => movie.Name.Contains(keyword));
        }

        var items = await dbQuery
            .OrderBy(movie => movie.Status == MovieStatus.NowShowing ? 0 : 1)
            .ThenBy(movie => movie.Name)
            .Take(query.MaxItems)
            .Select(movie => new MovieDropdownDto(movie.Id, movie.Name))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates upcoming/now-showing movie dropdown query payload.
/// </summary>
public class GetUpcomingAndNowShowingMovieDropdownValidator : AbstractValidator<GetUpcomingAndNowShowingMovieDropdownQuery>
{
    public GetUpcomingAndNowShowingMovieDropdownValidator()
    {
        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

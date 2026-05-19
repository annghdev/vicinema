using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets movies for dropdown data source.
/// </summary>
public class GetMovieDropdownQuery : ICachableQuery<IReadOnlyList<MovieDropdownDto>>
{
    public string? SearchTerm { get; set; }
    public MovieStatus? Status { get; set; }
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => MovieCacheKeys.GetMovieDropdown(SearchTerm, Status, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight movie dropdown list.
/// </summary>
public class GetMovieDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<MovieDropdownDto>> Handle(GetMovieDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Movies
            .GetQueryFilter();

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(movie => movie.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(movie => movie.Name.Contains(keyword));
        }

        var items = await dbQuery
            .OrderBy(movie => movie.Name)
            .Take(query.MaxItems)
            .Select(movie => new MovieDropdownDto(movie.Id, movie.Name))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates dropdown query payload.
/// </summary>
public class GetMovieDropdownValidator : AbstractValidator<GetMovieDropdownQuery>
{
    public GetMovieDropdownValidator()
    {
        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined(status.Value))
            .WithMessage("Invalid movie status.");
    }
}

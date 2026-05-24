using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets movies with pagination, filtering, and sorting.
/// </summary>
public class GetPagedMoviesQuery : ICachableQuery<PagedResult<MovieDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public MovieStatus? Status { get; set; }
    public MovieGenre? Genre { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => MovieCacheKeys.GetPagedMovies(PageNumber, PageSize, SearchTerm, Status, Genre, SortBy, SortDirection);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}

/// <summary>
/// Handles query for paged movie list.
/// </summary>
public class GetPagedMoviesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns paged movies after applying filter and sorting.
    /// </summary>
    public async Task<PagedResult<MovieDto>> Handle(GetPagedMoviesQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Movies
            .GetQueryFilter();

        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
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
                movie.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<MovieDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<Movie> ApplyFilter(IQueryable<Movie> dbQuery, GetPagedMoviesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(movie =>
                movie.Name.Contains(keyword) ||
                movie.Studio.Contains(keyword) ||
                movie.Director.Contains(keyword));
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(movie => movie.Status == query.Status.Value);
        }

        if (query.Genre.HasValue)
        {
            dbQuery = dbQuery.Where(movie => movie.Genre == query.Genre.Value);
        }

        return dbQuery;
    }

    private static IQueryable<Movie> ApplySorting(IQueryable<Movie> dbQuery, GetPagedMoviesQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("name", true) => dbQuery.OrderByDescending(movie => movie.Name),
            ("name", false) => dbQuery.OrderBy(movie => movie.Name),

            ("duration", true) => dbQuery.OrderByDescending(movie => movie.Duration),
            ("duration", false) => dbQuery.OrderBy(movie => movie.Duration),

            ("status", true) => dbQuery.OrderByDescending(movie => movie.Status),
            ("status", false) => dbQuery.OrderBy(movie => movie.Status),

            ("genre", true) => dbQuery.OrderByDescending(movie => movie.Genre),
            ("genre", false) => dbQuery.OrderBy(movie => movie.Genre),

            ("createdat", true) => dbQuery.OrderByDescending(movie => movie.CreatedAt),
            ("createdat", false) => dbQuery.OrderBy(movie => movie.CreatedAt),

            (_, true) => dbQuery.OrderByDescending(movie => movie.CreatedAt),
            _ => dbQuery.OrderBy(movie => movie.CreatedAt)
        };
    }
}

/// <summary>
/// Validates paged movie query payload.
/// </summary>
public class GetPagedMoviesValidator : AbstractValidator<GetPagedMoviesQuery>
{
    private static readonly string[] SupportedSortBy = ["name", "duration", "status", "genre", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPagedMoviesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .NotEmpty().WithMessage("SortBy is required.")
            .Must(sortBy => SupportedSortBy.Contains(sortBy.Trim().ToLowerInvariant()))
            .WithMessage($"SortBy is invalid. Supported values: {string.Join(", ", SupportedSortBy)}.");

        RuleFor(x => x.SortDirection)
            .NotEmpty().WithMessage("SortDirection is required.")
            .Must(direction => SupportedSortDirections.Contains(direction.Trim().ToLowerInvariant()))
            .WithMessage("SortDirection is invalid. Supported values: asc, desc.");

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined(status.Value))
            .WithMessage("Invalid movie status.");

        RuleFor(x => x.Genre)
            .Must(genre => genre is null || Enum.IsDefined(genre.Value))
            .WithMessage("Invalid movie genre.");
    }
}

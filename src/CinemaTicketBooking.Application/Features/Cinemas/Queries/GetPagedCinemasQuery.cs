using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets cinemas with pagination, filtering, and sorting.
/// </summary>
public class GetPagedCinemasQuery : ICachableQuery<PagedResult<CinemaDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => CinemaCacheKeys.GetPagedCinemas(PageNumber, PageSize, SearchTerm, IsActive, SortBy, SortDirection);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for paged cinema list.
/// </summary>
public class GetPagedCinemasHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns paged cinemas after applying filter and sorting.
    /// </summary>
    public async Task<PagedResult<CinemaDto>> Handle(GetPagedCinemasQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Cinemas
            .GetQueryFilter();

        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(cinema => new CinemaDto(
                cinema.Id,
                cinema.Name,
                cinema.ThumbnailUrl,
                cinema.Geo,
                cinema.Address,
                cinema.IsActive,
                cinema.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<CinemaDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<Cinema> ApplyFilter(IQueryable<Cinema> dbQuery, GetPagedCinemasQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(cinema =>
                cinema.Name.Contains(keyword) ||
                cinema.Address.Contains(keyword));
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(cinema => cinema.IsActive == query.IsActive.Value);
        }

        return dbQuery;
    }

    private static IQueryable<Cinema> ApplySorting(IQueryable<Cinema> dbQuery, GetPagedCinemasQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("name", true) => dbQuery.OrderByDescending(cinema => cinema.Name),
            ("name", false) => dbQuery.OrderBy(cinema => cinema.Name),

            ("address", true) => dbQuery.OrderByDescending(cinema => cinema.Address),
            ("address", false) => dbQuery.OrderBy(cinema => cinema.Address),

            ("isactive", true) => dbQuery.OrderByDescending(cinema => cinema.IsActive),
            ("isactive", false) => dbQuery.OrderBy(cinema => cinema.IsActive),

            ("createdat", true) => dbQuery.OrderByDescending(cinema => cinema.CreatedAt),
            ("createdat", false) => dbQuery.OrderBy(cinema => cinema.CreatedAt),

            (_, true) => dbQuery.OrderByDescending(cinema => cinema.CreatedAt),
            _ => dbQuery.OrderBy(cinema => cinema.CreatedAt)
        };
    }
}

/// <summary>
/// Validates paged cinema query payload.
/// </summary>
public class GetPagedCinemasValidator : AbstractValidator<GetPagedCinemasQuery>
{
    private static readonly string[] SupportedSortBy = ["name", "address", "isactive", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPagedCinemasValidator()
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
    }
}

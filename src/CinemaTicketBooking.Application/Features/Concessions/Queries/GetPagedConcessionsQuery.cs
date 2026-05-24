using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets concession items with pagination, filtering, and sorting.
/// </summary>
public class GetPagedConcessionsQuery : ICachableQuery<PagedResult<ConcessionDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsAvailable { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ConcessionCacheKeys.GetPagedConcessions(PageNumber, PageSize, SearchTerm, IsAvailable, SortBy, SortDirection);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for paged concession list.
/// </summary>
public class GetPagedConcessionsHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns paged concessions after applying filter and sorting.
    /// </summary>
    public async Task<PagedResult<ConcessionDto>> Handle(GetPagedConcessionsQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Concessions.GetQueryFilter();
        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(x => new ConcessionDto(
                x.Id,
                x.Name,
                x.Price,
                x.ImageUrl,
                x.IsAvailable,
                x.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ConcessionDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<Concession> ApplyFilter(IQueryable<Concession> dbQuery, GetPagedConcessionsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(x => x.Name.Contains(keyword));
        }

        if (query.IsAvailable.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.IsAvailable == query.IsAvailable.Value);
        }

        return dbQuery;
    }

    private static IQueryable<Concession> ApplySorting(IQueryable<Concession> dbQuery, GetPagedConcessionsQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("name", true) => dbQuery.OrderByDescending(x => x.Name),
            ("name", false) => dbQuery.OrderBy(x => x.Name),

            ("price", true) => dbQuery.OrderByDescending(x => x.Price),
            ("price", false) => dbQuery.OrderBy(x => x.Price),

            ("isavailable", true) => dbQuery.OrderByDescending(x => x.IsAvailable),
            ("isavailable", false) => dbQuery.OrderBy(x => x.IsAvailable),

            ("createdat", true) => dbQuery.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => dbQuery.OrderBy(x => x.CreatedAt),

            (_, true) => dbQuery.OrderByDescending(x => x.CreatedAt),
            _ => dbQuery.OrderBy(x => x.CreatedAt)
        };
    }
}

/// <summary>
/// Validates paged concession query payload.
/// </summary>
public class GetPagedConcessionsValidator : AbstractValidator<GetPagedConcessionsQuery>
{
    private static readonly string[] SupportedSortBy = ["name", "price", "isavailable", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPagedConcessionsValidator()
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

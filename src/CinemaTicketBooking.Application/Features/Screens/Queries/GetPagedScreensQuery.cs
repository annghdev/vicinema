using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets screens with pagination, filtering, and sorting.
/// </summary>
public class GetPagedScreensQuery : ICachableQuery<PagedResult<ScreenDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CinemaId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public ScreenType? Type { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ScreenCacheKeys.GetPagedScreens(PageNumber, PageSize, CinemaId, Type, IsActive, SortBy, SortDirection);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for paged screen list.
/// </summary>
public class GetPagedScreensHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns paged screens after applying filter and sorting.
    /// </summary>
    public async Task<PagedResult<ScreenDto>> Handle(GetPagedScreensQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Screens.GetQueryFilter();
        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(screen => new ScreenDto(
                screen.Id,
                screen.CinemaId,
                screen.Code,
                screen.RowOfSeats,
                screen.ColumnOfSeats,
                screen.TotalSeats,
                screen.SeatMap,
                screen.SupportedFormats,
                screen.IsActive,
                screen.Seats.Count(seat => seat.IsActive),
                screen.CreatedAt
            ))
            .ToListAsync(ct);

        return new PagedResult<ScreenDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<Screen> ApplyFilter(IQueryable<Screen> dbQuery, GetPagedScreensQuery query)
    {
        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(screen => screen.CinemaId == query.CinemaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(screen => screen.Code.Contains(keyword));
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(screen => screen.IsActive == query.IsActive.Value);
        }

        if (query.Type.HasValue)
        {
            // PostgreSQL EF provider translates Contains on JSON array
            dbQuery = dbQuery.Where(screen => screen.SupportedFormats.Contains(query.Type.Value));
        }

        return dbQuery;
    }

    private static IQueryable<Screen> ApplySorting(IQueryable<Screen> dbQuery, GetPagedScreensQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("code", true) => dbQuery.OrderByDescending(screen => screen.Code),
            ("code", false) => dbQuery.OrderBy(screen => screen.Code),

            ("isactive", true) => dbQuery.OrderByDescending(screen => screen.IsActive),
            ("isactive", false) => dbQuery.OrderBy(screen => screen.IsActive),

            ("type", true) => dbQuery.OrderByDescending(screen => screen.Type),
            ("type", false) => dbQuery.OrderBy(screen => screen.Type),

            ("createdat", true) => dbQuery.OrderByDescending(screen => screen.CreatedAt),
            ("createdat", false) => dbQuery.OrderBy(screen => screen.CreatedAt),

            (_, true) => dbQuery.OrderByDescending(screen => screen.CreatedAt),
            _ => dbQuery.OrderBy(screen => screen.CreatedAt)
        };
    }
}

/// <summary>
/// Validates paged screen query payload.
/// </summary>
public class GetPagedScreensValidator : AbstractValidator<GetPagedScreensQuery>
{
    private static readonly string[] SupportedSortBy = ["code", "isactive", "type", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPagedScreensValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");

        RuleFor(x => x.SortBy)
            .NotEmpty().WithMessage("SortBy is required.")
            .Must(sortBy => SupportedSortBy.Contains(sortBy.Trim().ToLowerInvariant()))
            .WithMessage($"SortBy is invalid. Supported values: {string.Join(", ", SupportedSortBy)}.");

        RuleFor(x => x.SortDirection)
            .NotEmpty().WithMessage("SortDirection is required.")
            .Must(direction => SupportedSortDirections.Contains(direction.Trim().ToLowerInvariant()))
            .WithMessage("SortDirection is invalid. Supported values: asc, desc.");

        RuleFor(x => x.Type)
            .Must(type => type is null || Enum.IsDefined(type.Value))
            .WithMessage("Invalid screen type.");
    }
}

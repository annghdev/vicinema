using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets pricing policies with pagination, filtering, and sorting.
/// </summary>
public class GetPagedPricingPoliciesQuery : ICachableQuery<PagedResult<PricingPolicyDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? CinemaId { get; set; }
    public ScreenType? ScreenType { get; set; }
    public SeatType? SeatType { get; set; }
    public bool? IsActive { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PricingPolicyCacheKeys.GetPagedPricingPolicies(PageNumber, PageSize, CinemaId, ScreenType, SeatType, IsActive, SortBy, SortDirection);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for paged pricing policy list.
/// </summary>
public class GetPagedPricingPoliciesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns paged pricing policies after applying filter and sorting.
    /// </summary>
    public async Task<PagedResult<PricingPolicyDto>> Handle(GetPagedPricingPoliciesQuery query, CancellationToken ct)
    {
        var dbQuery = uow.PricingPolicies.GetQueryFilter();
        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(x => new PricingPolicyDto(
                x.Id,
                x.CinemaId,
                x.ScreenType,
                x.SeatType,
                x.BasePrice,
                x.ScreenCoefficient,
                x.WeekendCoefficient,
                x.BasePrice * x.ScreenCoefficient,
                x.IsActive,
                x.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<PricingPolicyDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<PricingPolicy> ApplyFilter(IQueryable<PricingPolicy> dbQuery, GetPagedPricingPoliciesQuery query)
    {
        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CinemaId == query.CinemaId.Value);
        }

        if (query.ScreenType.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ScreenType == query.ScreenType.Value);
        }

        if (query.SeatType.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.SeatType == query.SeatType.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.IsActive == query.IsActive.Value);
        }

        return dbQuery;
    }

    private static IQueryable<PricingPolicy> ApplySorting(IQueryable<PricingPolicy> dbQuery, GetPagedPricingPoliciesQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("screentype", true) => dbQuery.OrderByDescending(x => x.ScreenType),
            ("screentype", false) => dbQuery.OrderBy(x => x.ScreenType),

            ("seattype", true) => dbQuery.OrderByDescending(x => x.SeatType),
            ("seattype", false) => dbQuery.OrderBy(x => x.SeatType),

            ("baseprice", true) => dbQuery.OrderByDescending(x => x.BasePrice),
            ("baseprice", false) => dbQuery.OrderBy(x => x.BasePrice),

            ("screencoefficient", true) => dbQuery.OrderByDescending(x => x.ScreenCoefficient),
            ("screencoefficient", false) => dbQuery.OrderBy(x => x.ScreenCoefficient),

            ("isactive", true) => dbQuery.OrderByDescending(x => x.IsActive),
            ("isactive", false) => dbQuery.OrderBy(x => x.IsActive),

            ("createdat", true) => dbQuery.OrderByDescending(x => x.CreatedAt),
            ("createdat", false) => dbQuery.OrderBy(x => x.CreatedAt),

            (_, true) => dbQuery.OrderByDescending(x => x.CreatedAt),
            _ => dbQuery.OrderBy(x => x.CreatedAt)
        };
    }
}

/// <summary>
/// Validates paged pricing policy query payload.
/// </summary>
public class GetPagedPricingPoliciesValidator : AbstractValidator<GetPagedPricingPoliciesQuery>
{
    private static readonly string[] SupportedSortBy =
        ["screentype", "seattype", "baseprice", "screencoefficient", "isactive", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPagedPricingPoliciesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");

        RuleFor(x => x.ScreenType)
            .Must(screenType => screenType is null || Enum.IsDefined(screenType.Value))
            .WithMessage("Invalid screen type.");

        RuleFor(x => x.SeatType)
            .Must(seatType => seatType is null || Enum.IsDefined(seatType.Value))
            .WithMessage("Invalid seat type.");

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

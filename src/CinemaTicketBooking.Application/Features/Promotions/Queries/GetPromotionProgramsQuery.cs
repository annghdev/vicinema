using CinemaTicketBooking.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features.Promotions;

public class GetPromotionProgramsQuery : ICachableQuery<PagedResult<PromotionProgramDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public string SortBy { get; set; } = "createdAt";
    public string SortDirection { get; set; } = "desc";
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => $"{PromotionCacheKeys.ListPrefix}Paged_{PageNumber}_{PageSize}_{SearchTerm}_{IsActive}_{SortBy}_{SortDirection}";
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}

public class GetPromotionProgramsHandler(IUnitOfWork uow)
{
    public async Task<PagedResult<PromotionProgramDto>> Handle(GetPromotionProgramsQuery query, CancellationToken ct)
    {
        var dbQuery = uow.PromotionPrograms.GetQueryFilter();

        dbQuery = ApplyFilter(dbQuery, query);
        dbQuery = ApplySorting(dbQuery, query);

        var totalItems = await dbQuery.CountAsync(ct);
        var skip = (query.PageNumber - 1) * query.PageSize;

        var items = await dbQuery
            .Skip(skip)
            .Take(query.PageSize)
            .Select(p => new PromotionProgramDto(
                p.Id,
                p.Name,
                p.Description,
                p.PosterImage,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.DiscountType.ToString(),
                p.DiscountForm.HasValue ? p.DiscountForm.Value.ToString() : null,
                p.DiscountValue,
                p.MaxDiscountAmount,
                p.MaxDiscountPercentage,
                p.MaxUsagePerCustomer,
                p.Conditions.Count,
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<PromotionProgramDto>(items, totalItems, query.PageNumber, query.PageSize);
    }

    private static IQueryable<PromotionProgram> ApplyFilter(IQueryable<PromotionProgram> dbQuery, GetPromotionProgramsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(p => p.Name.Contains(keyword));
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(p => p.IsActive == query.IsActive.Value);
        }

        return dbQuery;
    }

    private static IQueryable<PromotionProgram> ApplySorting(IQueryable<PromotionProgram> dbQuery, GetPromotionProgramsQuery query)
    {
        var sortBy = query.SortBy.Trim().ToLowerInvariant();
        var isDesc = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy, isDesc) switch
        {
            ("name", true) => dbQuery.OrderByDescending(p => p.Name),
            ("name", false) => dbQuery.OrderBy(p => p.Name),
            ("startdate", true) => dbQuery.OrderByDescending(p => p.StartDate),
            ("startdate", false) => dbQuery.OrderBy(p => p.StartDate),
            ("enddate", true) => dbQuery.OrderByDescending(p => p.EndDate),
            ("enddate", false) => dbQuery.OrderBy(p => p.EndDate),
            ("isactive", true) => dbQuery.OrderByDescending(p => p.IsActive),
            ("isactive", false) => dbQuery.OrderBy(p => p.IsActive),
            ("discountvalue", true) => dbQuery.OrderByDescending(p => p.DiscountValue),
            ("discountvalue", false) => dbQuery.OrderBy(p => p.DiscountValue),
            ("createdat", true) => dbQuery.OrderByDescending(p => p.CreatedAt),
            (_, true) => dbQuery.OrderByDescending(p => p.CreatedAt),
            _ => dbQuery.OrderBy(p => p.CreatedAt)
        };
    }
}

public class GetPromotionProgramsValidator : AbstractValidator<GetPromotionProgramsQuery>
{
    private static readonly string[] SupportedSortBy = ["name", "startdate", "enddate", "isactive", "discountvalue", "createdat"];
    private static readonly string[] SupportedSortDirections = ["asc", "desc"];

    public GetPromotionProgramsValidator()
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

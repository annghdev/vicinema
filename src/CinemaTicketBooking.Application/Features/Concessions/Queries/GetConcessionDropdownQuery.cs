using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets concessions for dropdown data source.
/// </summary>
public class GetConcessionDropdownQuery : ICachableQuery<IReadOnlyList<ConcessionDropdownDto>>
{
    public string? SearchTerm { get; set; }
    public bool OnlyAvailable { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ConcessionCacheKeys.GetConcessionDropdown(SearchTerm, OnlyAvailable, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight concession dropdown list.
/// </summary>
public class GetConcessionDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<ConcessionDropdownDto>> Handle(GetConcessionDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Concessions.GetQueryFilter();

        if (query.OnlyAvailable)
        {
            dbQuery = dbQuery.Where(x => x.IsAvailable);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(x => x.Name.Contains(keyword));
        }

        var items = await dbQuery
            .OrderBy(x => x.Name)
            .Take(query.MaxItems)
            .Select(x => new ConcessionDropdownDto(x.Id, x.Name))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates dropdown query payload.
/// </summary>
public class GetConcessionDropdownValidator : AbstractValidator<GetConcessionDropdownQuery>
{
    public GetConcessionDropdownValidator()
    {
        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

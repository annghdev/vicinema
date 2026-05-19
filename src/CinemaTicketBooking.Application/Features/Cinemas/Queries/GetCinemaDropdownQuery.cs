using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets cinemas for dropdown data source.
/// </summary>
public class GetCinemaDropdownQuery : ICachableQuery<IReadOnlyList<CinemaDropdownDto>>
{
    public string? SearchTerm { get; set; }
    public bool OnlyActive { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => CinemaCacheKeys.GetCinemaDropdown(SearchTerm, OnlyActive, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight cinema dropdown list.
/// </summary>
public class GetCinemaDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<CinemaDropdownDto>> Handle(GetCinemaDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Cinemas
            .GetQueryFilter();

        if (query.OnlyActive)
        {
            dbQuery = dbQuery.Where(cinema => cinema.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(cinema => cinema.Name.Contains(keyword));
        }

        var items = await dbQuery
            .OrderBy(cinema => cinema.Name)
            .Take(query.MaxItems)
            .Select(cinema => new CinemaDropdownDto(cinema.Id, cinema.Name))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates dropdown query payload.
/// </summary>
public class GetCinemaDropdownValidator : AbstractValidator<GetCinemaDropdownQuery>
{
    public GetCinemaDropdownValidator()
    {
        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

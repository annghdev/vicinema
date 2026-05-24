using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets screens for dropdown data source, optionally filtered by cinema.
/// </summary>
public class GetScreenDropdownQuery : ICachableQuery<IReadOnlyList<ScreenDropdownDto>>
{
    public Guid? CinemaId { get; set; }
    public string? SearchTerm { get; set; }
    public bool OnlyActive { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ScreenCacheKeys.GetScreenDropdown(CinemaId, OnlyActive, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight screen dropdown list.
/// </summary>
public class GetScreenDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<ScreenDropdownDto>> Handle(GetScreenDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Screens.GetQueryFilter();

        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(screen => screen.CinemaId == query.CinemaId.Value);
        }

        if (query.OnlyActive)
        {
            dbQuery = dbQuery.Where(screen => screen.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var keyword = query.SearchTerm.Trim();
            dbQuery = dbQuery.Where(screen => screen.Code.Contains(keyword));
        }

        var items = await dbQuery
            .OrderBy(screen => screen.Code)
            .Take(query.MaxItems)
            .Select(screen => new ScreenDropdownDto(screen.Id, screen.Code, screen.CinemaId))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates dropdown query payload.
/// </summary>
public class GetScreenDropdownValidator : AbstractValidator<GetScreenDropdownQuery>
{
    public GetScreenDropdownValidator()
    {
        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");

        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

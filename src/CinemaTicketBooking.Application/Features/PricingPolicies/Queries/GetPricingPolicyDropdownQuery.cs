using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets pricing policies for dropdown data source.
/// </summary>
public class GetPricingPolicyDropdownQuery : ICachableQuery<IReadOnlyList<PricingPolicyDropdownDto>>
{
    public Guid? CinemaId { get; set; }
    public bool OnlyActive { get; set; } = true;
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PricingPolicyCacheKeys.GetPricingPolicyDropdown(CinemaId, OnlyActive);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight pricing-policy dropdown list.
/// </summary>
public class GetPricingPolicyDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<PricingPolicyDropdownDto>> Handle(GetPricingPolicyDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.PricingPolicies.GetQueryFilter();

        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CinemaId == query.CinemaId.Value);
        }

        if (query.OnlyActive)
        {
            dbQuery = dbQuery.Where(x => x.IsActive);
        }

        var items = await dbQuery
            .OrderBy(x => x.ScreenType)
            .ThenBy(x => x.SeatType)
            .Take(query.MaxItems)
            .Select(x => new PricingPolicyDropdownDto(
                x.Id,
                $"{x.ScreenType}-{x.SeatType} ({x.BasePrice * x.ScreenCoefficient})"))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates pricing-policy dropdown query payload.
/// </summary>
public class GetPricingPolicyDropdownValidator : AbstractValidator<GetPricingPolicyDropdownQuery>
{
    public GetPricingPolicyDropdownValidator()
    {
        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");

        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

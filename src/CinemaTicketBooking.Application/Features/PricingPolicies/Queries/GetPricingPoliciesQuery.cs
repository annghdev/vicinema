using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all pricing policies.
/// </summary>
public class GetPricingPoliciesQuery : ICachableQuery<IReadOnlyList<PricingPolicyDto>>
{
    public Guid? CinemaId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PricingPolicyCacheKeys.GetPricingPolicies(CinemaId);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for all pricing policies.
/// </summary>
public class GetPricingPoliciesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all pricing policies mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<PricingPolicyDto>> Handle(GetPricingPoliciesQuery query, CancellationToken ct)
    {
        var dbQuery = uow.PricingPolicies.GetQueryFilter();

        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CinemaId == query.CinemaId.Value);
        }

        var items = await dbQuery
            .OrderBy(x => x.ScreenType)
            .ThenBy(x => x.SeatType)
            .ThenBy(x => x.BasePrice)
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

        return items;
    }
}

/// <summary>
/// Validates pricing policy list query payload.
/// </summary>
public class GetPricingPoliciesValidator : AbstractValidator<GetPricingPoliciesQuery>
{
    public GetPricingPoliciesValidator()
    {
        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");
    }
}

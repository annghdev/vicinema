using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets a pricing policy by id.
/// </summary>
public class GetPricingPolicyByIdQuery : ICachableQuery<PricingPolicyDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PricingPolicyCacheKeys.GetPricingPolicyById(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for a specific pricing policy.
/// </summary>
public class GetPricingPolicyByIdHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns pricing policy data when found; otherwise null.
    /// </summary>
    public async Task<PricingPolicyDto?> Handle(GetPricingPolicyByIdQuery query, CancellationToken ct)
    {
        var item = await uow.PricingPolicies
            .GetQueryFilter()
            .Where(x => x.Id == query.Id)
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
            .FirstOrDefaultAsync(ct);

        return item;
    }
}

/// <summary>
/// Validates query payload.
/// </summary>
public class GetPricingPolicyByIdValidator : AbstractValidator<GetPricingPolicyByIdQuery>
{
    public GetPricingPolicyByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Pricing policy ID is required.");
    }
}

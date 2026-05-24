using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// Queries all loyalty tier configurations (active + inactive) for admin.
/// </summary>
public class GetAllLoyaltyTiersQuery : ICachableQuery<IReadOnlyList<LoyaltyTierDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => LoyaltyCacheKeys.ActiveTiers();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(30);
}

/// <summary>
/// Handles fetching all loyalty tier configurations (including inactive).
/// </summary>
public class GetAllLoyaltyTiersHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all tier configurations ordered by tier level.
    /// </summary>
    public async Task<IReadOnlyList<LoyaltyTierDto>> Handle(GetAllLoyaltyTiersQuery _, CancellationToken ct)
    {
        // Use the repository's GetQueryFilter for all tiers (not just active)
        var tiers = uow.LoyaltyTiers.GetQueryFilter()
            .OrderBy(t => t.Tier)
            .ToList();

        return tiers.Select(t => new LoyaltyTierDto(
            Id: t.Id,
            Tier: t.Tier,
            Name: t.Name,
            MinPoints: t.MinPoints,
            MaxPoints: t.MaxPoints,
            TicketDiscountPercent: t.TicketDiscountPercent,
            ConcessionDiscountPercent: t.ConcessionDiscountPercent,
            Description: t.Description,
            IsActive: t.IsActive)).ToList();
    }
}

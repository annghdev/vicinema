using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// Returns all active loyalty tier configurations for the public benefits page.
/// </summary>
public class GetActiveLoyaltyTiersQuery : ICachableQuery<IReadOnlyList<LoyaltyTierDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => LoyaltyCacheKeys.ActiveTiers();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(30);
}

/// <summary>
/// Handles fetching active loyalty tiers.
/// </summary>
public class GetActiveLoyaltyTiersHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all active tier configurations ordered by tier level.
    /// </summary>
    public async Task<IReadOnlyList<LoyaltyTierDto>> Handle(GetActiveLoyaltyTiersQuery _, CancellationToken ct)
    {
        var tiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);

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

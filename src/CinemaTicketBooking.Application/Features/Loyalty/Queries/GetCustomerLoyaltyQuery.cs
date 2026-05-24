using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// Returns the current customer's loyalty status: tier, points, and next-tier progress.
/// </summary>
public class GetCustomerLoyaltyQuery : ICachableQuery<CustomerLoyaltyDto?>
{
    public Guid CustomerId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => LoyaltyCacheKeys.CustomerLoyalty(CustomerId);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}

/// <summary>
/// Handles customer loyalty lookup with cache support.
/// </summary>
public class GetCustomerLoyaltyHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Builds a customer loyalty DTO including current tier info and next-tier progress.
    /// </summary>
    public async Task<CustomerLoyaltyDto?> Handle(GetCustomerLoyaltyQuery query, CancellationToken ct)
    {
        // 1. Load customer
        var customer = await uow.Customers.GetByIdAsync(query.CustomerId, ct);
        if (customer is null || !customer.IsRegistered)
            return null;

        // 2. Load active tier configurations
        var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);

        // 3. Find current tier config
        var currentTier = activeTiers.FirstOrDefault(t => t.Tier == customer.LoyaltyTier);
        if (currentTier is null)
            return null;

        // 4. Find next tier (higher tier level)
        var nextTier = activeTiers
            .Where(t => t.Tier > customer.LoyaltyTier)
            .OrderBy(t => t.Tier)
            .FirstOrDefault();

        // 5. Calculate points needed for next tier
        int? pointsToNextTier = nextTier is not null
            ? nextTier.MinPoints - customer.AccumulatedPoints
            : null;

        return new CustomerLoyaltyDto(
            CustomerId: customer.Id,
            CurrentTier: customer.LoyaltyTier,
            TierName: currentTier.Name,
            AccumulatedPoints: customer.AccumulatedPoints,
            PointsToNextTier: pointsToNextTier,
            TicketDiscountPercent: currentTier.TicketDiscountPercent,
            ConcessionDiscountPercent: currentTier.ConcessionDiscountPercent,
            NextTier: nextTier?.Tier);
    }
}

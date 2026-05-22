using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Features.Loyalty;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Invalidates loyalty cache entries when loyalty data changes.
/// </summary>
public class LoyaltyCacheInvalidationHandler(ICacheService cacheService)
{
    /// <summary>
    /// Invalidates customer loyalty cache when points are earned.
    /// </summary>
    public async Task Handle(LoyaltyPointsEarned @event, CancellationToken ct)
    {
        await cacheService.RemoveAsync(LoyaltyCacheKeys.CustomerLoyalty(@event.CustomerId), ct);
    }

    /// <summary>
    /// Invalidates customer loyalty cache when tier is upgraded.
    /// </summary>
    public async Task Handle(LoyaltyTierUpgraded @event, CancellationToken ct)
    {
        await cacheService.RemoveAsync(LoyaltyCacheKeys.CustomerLoyalty(@event.CustomerId), ct);
    }

    /// <summary>
    /// Invalidates tier list cache when a tier configuration is updated.
    /// </summary>
    public async Task Handle(LoyaltyTierConfigurationUpdated @event, CancellationToken ct)
    {
        await cacheService.RemoveAsync(LoyaltyCacheKeys.ActiveTiers(), ct);
    }
}

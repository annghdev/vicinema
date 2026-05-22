namespace CinemaTicketBooking.Domain.Repositories;

/// <summary>
/// Repository interface for <see cref="LoyaltyTierConfiguration"/>.
/// </summary>
public interface ILoyaltyTierConfigurationRepository : IRepository<LoyaltyTierConfiguration>
{
    /// <summary>
    /// Returns all active tier configurations, ordered by tier level ascending.
    /// </summary>
    Task<IReadOnlyList<LoyaltyTierConfiguration>> GetActiveTiersAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a single tier configuration by tier level, or null.
    /// </summary>
    Task<LoyaltyTierConfiguration?> GetByTierAsync(LoyaltyTier tier, CancellationToken ct = default);
}

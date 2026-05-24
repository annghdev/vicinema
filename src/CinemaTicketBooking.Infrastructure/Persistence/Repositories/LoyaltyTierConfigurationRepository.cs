using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for <see cref="LoyaltyTierConfiguration"/>.
/// </summary>
public class LoyaltyTierConfigurationRepository(AppDbContext db)
    : BaseRepository<LoyaltyTierConfiguration>(db), ILoyaltyTierConfigurationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<LoyaltyTierConfiguration>> GetActiveTiersAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .Where(x => x.IsActive && x.DeletedAt == null)
            .OrderBy(x => x.Tier)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public Task<LoyaltyTierConfiguration?> GetByTierAsync(LoyaltyTier tier, CancellationToken ct = default)
    {
        return _dbSet.FirstOrDefaultAsync(x => x.Tier == tier, ct);
    }
}

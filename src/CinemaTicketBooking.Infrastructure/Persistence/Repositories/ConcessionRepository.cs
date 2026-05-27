using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence;

/// <summary>
/// Repository for Concession entities. Handles CRUD and batch ID lookups.
/// </summary>
public class ConcessionRepository(AppDbContext db) : BaseRepository<Concession>(db), IConcessionRepository
{
    /// <summary>
    /// Loads concessions by a list of IDs in a single query.
    /// </summary>
    public Task<List<Concession>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        return _dbSet.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
    }
}

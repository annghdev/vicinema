using CinemaTicketBooking.Domain.Repositories;
using CinemaTicketBooking.Domain;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence.Repositories;

public class PromotionProgramRepository(AppDbContext context) : BaseRepository<PromotionProgram>(context), IPromotionProgramRepository
{
    public async Task<IReadOnlyList<PromotionProgram>> GetActiveWithConditionsAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Where(p => p.IsActive
                && p.StartDate <= DateTimeOffset.UtcNow
                && p.EndDate >= DateTimeOffset.UtcNow
                && p.DeletedAt == null)
            .Include(p => p.Conditions)
            .Include(p => p.FreeConcessionItems)
            .ToListAsync(ct);
    }

    public async Task<PromotionProgram?> GetByIdWithConditionsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(p => p.Conditions)
            .Include(p => p.FreeConcessionItems)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<IReadOnlyList<PromotionProgram>> GetAllWithConditionsAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .IgnoreQueryFilters()
            .Include(p => p.Conditions)
            .Include(p => p.FreeConcessionItems)
            .ToListAsync(ct);
    }
}

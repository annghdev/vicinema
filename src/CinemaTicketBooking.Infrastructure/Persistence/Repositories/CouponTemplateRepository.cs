using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence.Repositories;

public class CouponTemplateRepository(AppDbContext dbContext) : BaseRepository<CouponTemplate>(dbContext), ICouponTemplateRepository
{
    public async Task<CouponTemplate?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        return await dbContext.CouponTemplates
            .FirstOrDefaultAsync(t => t.Code == code, ct);
    }

    public async Task<IReadOnlyList<CouponTemplate>> GetActivePublicTemplatesAsync(CancellationToken ct = default)
    {
        return await dbContext.CouponTemplates
            .Where(t => t.IsActive
                && t.Type == CouponType.Public)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CouponTemplate>> GetAllOrderedAsync(CancellationToken ct = default)
    {
        return await dbContext.CouponTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CouponTemplate>> GetPersonalTemplatesAsync(CancellationToken ct = default)
    {
        return await dbContext.CouponTemplates
            .Where(t => t.Type == CouponType.Personal)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }
}
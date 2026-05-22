using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Infrastructure.Persistence.Repositories;

public class CustomerCouponRepository(AppDbContext dbContext) : BaseRepository<CustomerCoupon>(dbContext), ICustomerCouponRepository
{
    public async Task<IReadOnlyList<CustomerCoupon>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
    {
        return await dbContext.CustomerCoupons
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);
    }

    public async Task<CustomerCoupon?> GetByCustomerAndCodeAsync(Guid customerId, string couponCode, CancellationToken ct = default)
    {
        return await dbContext.CustomerCoupons
            .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CouponCode == couponCode, ct);
    }

    public async Task<IReadOnlyList<CustomerCoupon>> GetAvailableByCustomerAsync(Guid customerId, DateTimeOffset now, CancellationToken ct = default)
    {
        return await dbContext.CustomerCoupons
            .Where(c => c.CustomerId == customerId
                && c.UsageCount < c.MaxUsage
                && c.ExpiresAt >= now)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync(ct);
    }

    public async Task<int> GetUsageCountByCustomerAndCodeAsync(Guid customerId, string couponCode, CancellationToken ct = default)
    {
        return await dbContext.CustomerCoupons
            .CountAsync(c => c.CustomerId == customerId && c.CouponCode == couponCode, ct);
    }
}
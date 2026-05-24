using CinemaTicketBooking.Domain.Repositories;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Infrastructure.Persistence.Repositories;

public class CustomerPromotionUsageRepository(AppDbContext context) : BaseRepository<CustomerPromotionUsage>(context), ICustomerPromotionUsageRepository
{
}

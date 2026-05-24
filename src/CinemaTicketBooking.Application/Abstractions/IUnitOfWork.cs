using CinemaTicketBooking.Domain.Repositories;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Abstractions;

public interface IUnitOfWork
{
    ICinemaRepository Cinemas { get; }
    IMovieRepository Movies { get; }
    IBookingRepository Bookings { get; }
    ITicketRepository Tickets { get; }
    IShowTimeRepository ShowTimes { get; }
    IScreenRepository Screens { get; }
    IConcessionRepository Concessions { get; }
    ICustomerRepository Customers { get; }
    IPricingPolicyRepository PricingPolicies { get; }
    ISeatSelectionPolicyRepository SeatSelectionPolicies { get; }
    IPaymentTransactionRepository PaymentTransactions { get; }
    ISlideRepository Slides { get; }
    ILoyaltyTierConfigurationRepository LoyaltyTiers { get; }
    ICouponTemplateRepository CouponTemplates { get; }
    ICustomerCouponRepository CustomerCoupons { get; }
    IPromotionProgramRepository PromotionPrograms { get; }
    ICustomerPromotionUsageRepository CustomerPromotionUsages { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);
}

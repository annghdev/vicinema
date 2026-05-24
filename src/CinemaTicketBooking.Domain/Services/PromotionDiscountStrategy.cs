using CinemaTicketBooking.Domain.Services;

namespace CinemaTicketBooking.Domain;

public class PromotionDiscountStrategy : IDiscountStrategy
{
    public int Priority => 20;

    public LoyaltyDiscountResult? Calculate(
        Booking booking,
        Customer customer,
        IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
    {
        if (booking.AppliedPromotions.Count == 0)
            return null;

        return new LoyaltyDiscountResult(
            TicketDiscount: booking.TotalPromotionDiscount,
            ConcessionDiscount: 0);
    }
}

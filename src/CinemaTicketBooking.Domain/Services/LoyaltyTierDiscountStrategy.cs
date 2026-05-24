namespace CinemaTicketBooking.Domain.Services;

/// <summary>
/// Applies tier-based loyalty discount to a registered customer's booking.
/// Priority 0 — always runs first.
/// </summary>
public class LoyaltyTierDiscountStrategy(ILoyaltyDiscountService loyaltyService) : IDiscountStrategy
{
    public int Priority => 0;

    /// <inheritdoc />
    public LoyaltyDiscountResult? Calculate(
        Booking booking,
        Customer customer,
        IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
    {
        // 1. Only apply if customer is registered
        if (!customer.IsRegistered)
            return null;

        // 2. Find the tier configuration matching the customer's current tier
        var tierConfig = activeTiers.FirstOrDefault(t => t.Tier == customer.LoyaltyTier);
        if (tierConfig is null || !tierConfig.IsActive)
            return null;

        // 3. Calculate discount
        return loyaltyService.CalculateDiscount(booking, customer, tierConfig);
    }
}

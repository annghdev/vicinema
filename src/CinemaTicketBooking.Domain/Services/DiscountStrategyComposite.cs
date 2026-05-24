namespace CinemaTicketBooking.Domain.Services;

/// <summary>
/// Chains multiple <see cref="IDiscountStrategy"/> strategies, accumulating all non-null results.
/// Strategies are executed in priority order (lower = first).
/// </summary>
public class DiscountStrategyComposite(IEnumerable<IDiscountStrategy> strategies)
{
    /// <summary>
    /// Runs all strategies and returns the combined discount total.
    /// </summary>
    public decimal CalculateTotalDiscount(
        Booking booking,
        Customer customer,
        IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
    {
        var total = 0m;

        foreach (var strategy in strategies.OrderBy(s => s.Priority))
        {
            var result = strategy.Calculate(booking, customer, activeTiers);
            if (result is not null)
            {
                total += result.TicketDiscount + result.ConcessionDiscount;
            }
        }

        return total;
    }
}

using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Services;
using Microsoft.Extensions.Logging;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Handles <see cref="BookingConfirmed"/> — accumulates loyalty points and checks for tier upgrade.
/// </summary>
public class LoyaltyPointsAccumulationHandler(
    IUnitOfWork uow,
    ILoyaltyDiscountService loyaltyService,
    ILogger<LoyaltyPointsAccumulationHandler> logger)
{
    /// <summary>
    /// Wolverine message handler invoked after a booking is confirmed.
    /// </summary>
    public async Task Handle(BookingConfirmed @event, CancellationToken ct)
    {
        // 1. Skip guest bookings (no loyalty)
        if (!@event.CustomerId.HasValue)
            return;

        // 2. Load customer
        var customer = await uow.Customers.GetByIdAsync(@event.CustomerId.Value, ct);
        if (customer is null)
        {
            logger.LogWarning("BookingConfirmed {BookingId}: customer {CustomerId} not found — loyalty skipped.",
                @event.BookingId, @event.CustomerId);
            return;
        }

        // 3. Load active tier configurations
        var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);

        // 4. Calculate points from the final booking amount
        var points = loyaltyService.CalculatePoints(@event.FinalAmount);
        if (points <= 0)
            return;

        // 5. Add points to customer (raises LoyaltyPointsEarned)
        customer.AddPoints(points, @event.BookingId, @event.FinalAmount);

        // 6. Determine new tier and upgrade if changed
        var newTier = loyaltyService.DetermineTier(customer.AccumulatedPoints, activeTiers);
        if (newTier > customer.LoyaltyTier)
        {
            customer.UpgradeTier(newTier);
        }
        // 7. Persist
        await uow.CommitAsync(ct);
        logger.LogInformation(
            "Loyalty: Customer {CustomerId} earned {Points} pts (total: {Total}), tier: {Tier}",
            customer.Id, points, customer.AccumulatedPoints, customer.LoyaltyTier);
    }
}

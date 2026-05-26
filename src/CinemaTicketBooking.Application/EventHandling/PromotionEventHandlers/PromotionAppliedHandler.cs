using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Events;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Handles the PromotionApplied domain event after promotion is applied to a booking.
/// Records a CustomerPromotionUsage if the promotion has a MaxUsagePerCustomer limit
/// and the customer is identified (CustomerId is not null).
/// Runs after the booking transaction commits via Wolverine durable inbox.
/// </summary>
public class PromotionAppliedHandler(IUnitOfWork uow)
{
    /// <summary>
    /// 1. Skip if no registered customer (anonymous/guest customers are not tracked).
    /// 2. Fetch the promotion program to check MaxUsagePerCustomer.
    /// 3. If null, no usage limit — skip recording.
    /// 4. Otherwise, create and persist a CustomerPromotionUsage record.
    /// </summary>
    public async Task Handle(PromotionApplied @event, CancellationToken ct)
    {
        // 1. Skip anonymous/guest customers
        if (@event.CustomerId is null)
            return;

        // 2. Check if the promotion enforces per-customer usage limits
        var program = await uow.PromotionPrograms.GetByIdAsync(@event.PromotionProgramId, ct);
        if (program?.MaxUsagePerCustomer is null)
            return;

        // 3. Record the usage for this booking
        var usage = CustomerPromotionUsage.Create(
            customerId: @event.CustomerId.Value,
            promotionProgramId: @event.PromotionProgramId,
            bookingId: @event.BookingId);
        uow.CustomerPromotionUsages.Add(usage);
        await uow.CommitAsync(ct);
    }
}

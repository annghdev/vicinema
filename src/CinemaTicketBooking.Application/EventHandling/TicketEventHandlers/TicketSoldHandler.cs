namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Publishes realtime delta when a ticket is sold.
/// </summary>
public class RealtimeTicketSoldHandler(ITicketRealtimePublisher realtimePublisher)
{
    /// <summary>
    /// Pushes status change for sold tickets.
    /// </summary>
    public async Task Handle(TicketSold domainEvent, CancellationToken ct)
    {
        await realtimePublisher.PublishTicketStatusChangedAsync(
            new TicketStatusChangedRealtimeEvent(
                domainEvent.ShowTimeId,
                domainEvent.TicketId,
                domainEvent.TicketCode,
                TicketStatus.Sold,
                DateTimeOffset.UtcNow),
            ct);
    }
}

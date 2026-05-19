namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Publishes realtime delta when a ticket is released.
/// </summary>
public class RealtimeTicketReleasedHandler(ITicketRealtimePublisher realtimePublisher)
{
    /// <summary>
    /// Pushes status change for released tickets.
    /// </summary>
    public async Task Handle(TicketReleased domainEvent, CancellationToken ct)
    {
        await realtimePublisher.PublishTicketStatusChangedAsync(
            new TicketStatusChangedRealtimeEvent(
                domainEvent.ShowTimeId,
                domainEvent.TicketId,
                domainEvent.TicketCode,
                TicketStatus.Available,
                DateTimeOffset.UtcNow),
            ct);
    }
}

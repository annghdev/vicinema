using Microsoft.Extensions.Options;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Schedules auto-release when a ticket is locked.
/// </summary>
public class TimeoutSchedulingTicketLockedHandler(
    IMessageBus bus,
    IOptions<TicketLockingOptions> options)
{
    /// <summary>
    /// Persists delayed timeout release through Wolverine scheduling.
    /// </summary>
    public async Task Handle(TicketLocked domainEvent, CancellationToken ct)
    {
        var executeAt = DateTimeOffset.UtcNow.Add(options.Value.LockHoldDuration);
        await bus.ScheduleAsync(
            new ReleaseTicketLockByTimeoutCommand
            {
                TicketId = domainEvent.TicketId,
                LockingBy = domainEvent.LockingBy,
                CorrelationId = domainEvent.TicketId.ToString()
            },
            executeAt);
    }
}

/// <summary>
/// Publishes a realtime event when a ticket is locked.
/// </summary>
public class RealtimeTicketLockedHandler(
    ITicketRealtimePublisher realtimePublisher)
{
    /// <summary>
    /// Handles the event when a ticket is locked and publishes a realtime update.
    /// </summary>
    public async Task Handle(TicketLocked domainEvent, CancellationToken ct)
    {
        await realtimePublisher.PublishTicketStatusChangedAsync(
            new TicketStatusChangedRealtimeEvent(
                domainEvent.ShowTimeId,
                domainEvent.TicketId,
                domainEvent.TicketCode,
                TicketStatus.Locking,
                DateTimeOffset.UtcNow,
                domainEvent.LockingBy),
            ct);
    }
}

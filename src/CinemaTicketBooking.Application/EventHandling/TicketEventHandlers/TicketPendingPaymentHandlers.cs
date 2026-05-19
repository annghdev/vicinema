namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Schedules auto-release when a ticket enters pending payment stage.
/// </summary>
public class TimeoutSchedulingTicketPendingPaymentHandler(IMessageBus bus)
{
    /// <summary>
    /// Persists delayed payment-timeout release through Wolverine scheduling.
    /// </summary>
    public async Task Handle(TicketPendingPayment domainEvent, CancellationToken ct)
    {
        await bus.ScheduleAsync(
            new ReleaseTicketPaymentByTimeoutCommand
            {
                TicketId = domainEvent.TicketId,
                BookingId = domainEvent.BookingId,
                CorrelationId = domainEvent.BookingId.ToString()
            },
            domainEvent.PaymentExpiresAt);
    }
}

/// <summary>
/// Publishes a realtime event when a ticket enters pending payment stage.
/// </summary>
public class RealtimeTicketPendingPaymentHandler(ITicketRealtimePublisher realtimePublisher)
{
    /// <summary>
    /// Publishes a realtime event when a ticket enters pending payment stage.
    /// </summary>
    public async Task Handle(TicketPendingPayment domainEvent, CancellationToken ct)
    {
        await realtimePublisher.PublishTicketStatusChangedAsync(
            new TicketStatusChangedRealtimeEvent(
                domainEvent.ShowTimeId,
                domainEvent.TicketId,
                domainEvent.TicketCode,
                TicketStatus.PendingPayment,
                DateTimeOffset.UtcNow),
            ct);
    }
}
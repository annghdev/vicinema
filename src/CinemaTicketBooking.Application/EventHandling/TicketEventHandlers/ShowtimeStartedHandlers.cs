using CinemaTicketBooking.Application.Features;

namespace CinemaTicketBooking.Application.Messaging;

public class StatusShowtimeStartedHandler(IMessageBus bus, IUnitOfWork uow)
{
    public async Task Handle(ShowTimeStarted @event, CancellationToken ct)
    {
        var showTime = await uow.ShowTimes.GetByIdAsync(@event.ShowTimeId, ct);
        if (showTime is null || showTime.Status != ShowTimeStatus.Showing)
        {
            return;
        }

        await bus.ScheduleAsync(new CompleteShowTimeCommand
        {
            Id = @event.ShowTimeId,
            CorrelationId = Guid.CreateVersion7().ToString()
        },
        showTime.EndAt);
    }
}

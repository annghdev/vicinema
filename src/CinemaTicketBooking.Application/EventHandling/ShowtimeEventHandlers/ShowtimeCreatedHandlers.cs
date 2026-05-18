using CinemaTicketBooking.Application.Features;

namespace CinemaTicketBooking.Application.Messaging;

public class StatusShowtimeCreatedHandler(IMessageBus bus, IUnitOfWork uow)
{
    public async Task Handle(ShowTimeCreated @event, CancellationToken ct)
    {
        var showTime = await uow.ShowTimes.GetByIdAsync(@event.ShowTimeId, ct);
        if (showTime is null || showTime.Status != ShowTimeStatus.Upcoming)
        {
            return;
        }

        await bus.ScheduleAsync(new StartShowTimeCommand
        {
            Id = @event.ShowTimeId,
            CorrelationId = Guid.CreateVersion7().ToString()
        },
        @event.StartAt);
    }
}

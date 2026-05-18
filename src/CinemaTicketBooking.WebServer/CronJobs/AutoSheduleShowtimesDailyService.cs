using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Wolverine;

namespace CinemaTicketBooking.WebServer.CronJobs;

/// <summary>
/// Background service to automatically schedule showtimes based on the latest scheduled day.
/// Runs daily at 3:00 AM (Vietnam Time - UTC+7).
/// </summary>
public class AutoSheduleShowtimesDailyService(
    IServiceScopeFactory scopeFactory,
    ILogger<AutoSheduleShowtimesDailyService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var vnOffset = TimeSpan.FromHours(7);
            var nowVn = DateTimeOffset.UtcNow.ToOffset(vnOffset);
            
            // 1. Calculate time until next 3:00 AM (VN)
            var nextRunDate = nowVn.Hour >= 3 ? nowVn.Date.AddDays(1) : nowVn.Date;
            var next3AMVn = new DateTimeOffset(nextRunDate.AddHours(3), vnOffset);
            var delay = next3AMVn - DateTimeOffset.UtcNow;
            
            logger.LogInformation("AutoSheduleShowtimesHostedService waiting for {Delay} until next run at {NextRun}", delay, next3AMVn);
            
            await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested)
                break;

            // 2. Execute scheduling
            try
            {
                await ScheduleNextDayShowtimesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while auto scheduling showtimes.");
            }
        }
    }

    // =============================================
    // Scheduling logic
    // =============================================

    /// <summary>
    /// Finds the latest scheduled day and copies all its showtimes to the next day.
    /// </summary>
    private async Task ScheduleNextDayShowtimesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // 1. Find the latest scheduled Date
        var latestDate = await dbContext.ShowTimes
            .MaxAsync(x => (DateOnly?)x.Date, cancellationToken);

        if (latestDate is null)
        {
            logger.LogWarning("No existing showtimes found. Cannot auto-schedule.");
            return;
        }

        // 2. Get all showtimes for that latest date
        var latestShowtimes = await dbContext.ShowTimes
            .Where(x => x.Date == latestDate.Value && x.Status != ShowTimeStatus.Cancelled)
            .ToListAsync(cancellationToken);

        logger.LogInformation("Found {Count} showtimes on the latest date {Date}. Scheduling for the next day...", latestShowtimes.Count, latestDate);

        // 3. Dispatch AddShowTimeCommand for the next day
        foreach (var st in latestShowtimes)
        {
            var command = new AddShowTimeCommand
            {
                MovieId = st.MovieId,
                ScreenId = st.ScreenId,
                StartAt = st.StartAt.AddDays(1),
                Format = st.Format,
                CorrelationId = Guid.CreateVersion7().ToString()
            };

            await messageBus.InvokeAsync<Guid>(command, cancellationToken);
        }

        logger.LogInformation("Successfully auto-scheduled showtimes for {NextDate}.", latestDate.Value.AddDays(1));
    }
}

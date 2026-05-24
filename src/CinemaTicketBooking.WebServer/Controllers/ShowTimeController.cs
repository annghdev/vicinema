using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Handles showtime management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class ShowTimeController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a calendar list of showtimes.
    /// </summary>
    [Authorize(Policy = Permissions.ShowTimesView)]
    public IActionResult Index(string? date = null)
    {
        ViewData["Title"] = "Showtime Calendar";

        DateOnly selectedDate;
        if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out selectedDate))
        {
            selectedDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        }

        var model = new ShowTimeCalendarViewModel
        {
            SelectedDate = selectedDate,
            Cinemas = [],
            Screens = [],
            ShowTimes = [],
            Movies = []
        };

        return View(model);
    }

    /// <summary>
    /// Processes a request to schedule a new Showtime via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ShowTimesManage)]
    public async Task<IActionResult> Create(Guid movieId, Guid screenId, DateTimeOffset startAt, ScreenType? format = null)
    {
        var command = new AddShowTimeCommand
        {
            MovieId = movieId,
            ScreenId = screenId,
            StartAt = startAt.ToUniversalTime(),
            Format = format
        };

        try
        {
            var showtimeId = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Lịch chiếu đã được tạo thành công!", id = showtimeId });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels an upcoming showtime via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ShowTimesManage)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new CancelShowTimeCommand { Id = id });
            return Json(new { success = true, message = "Đã hủy suất chiếu thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Displays seat map layout and booking history for specific showtime.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SeatSelection(Guid id, [FromQuery] string? date)
    {
        var showtime = await bus.InvokeAsync<ShowTimeDetailDto?>(new GetShowTimeByIdQuery { Id = id });
        if (showtime is null)
        {
            return NotFound("Không tìm thấy suất chiếu được yêu cầu.");
        }

        ViewData["ReturnDate"] = date;
        ViewData["Title"] = $"Tình trạng ghế: {showtime.MovieName}";
        return View(showtime);
    }

    /// <summary>
    /// Serves showtime seating details and transaction trails for administrative AJAX refresh.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSeatSelectionData(Guid id)
    {
        var showtime = await bus.InvokeAsync<ShowTimeDetailDto?>(new GetShowTimeByIdQuery { Id = id });
        if (showtime is null)
        {
            return Json(new { success = false, message = "Không tìm thấy suất chiếu." });
        }

        var bookingLogs = await bus.InvokeAsync<IReadOnlyList<ShowTimeBookingDto>>(new GetBookingHistoryByShowTimeIdQuery { ShowTimeId = id });

        return Json(new { success = true, showtime, bookingLogs });
    }

    /// <summary>
    /// Gets layout and showtimes for a specific date in JSON.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ShowTimesView)]
    public async Task<IActionResult> GetCalendarData(string date)
    {
        DateOnly selectedDate;
        if (string.IsNullOrEmpty(date) || !DateOnly.TryParse(date, out selectedDate))
        {
            selectedDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).DateTime);
        }

        var cinemasTask = bus.InvokeAsync<IReadOnlyList<CinemaDto>>(new GetCinemasQuery());
        var screensTask = bus.InvokeAsync<IReadOnlyList<ScreenDto>>(new GetScreensQuery());
        var showtimesTask = bus.InvokeAsync<IReadOnlyList<ShowTimeDto>>(new GetShowTimesQuery { Date = selectedDate });
        var moviesTask = bus.InvokeAsync<IReadOnlyList<MovieDropdownDto>>(new GetMovieDropdownQuery());

        await Task.WhenAll(cinemasTask, screensTask, showtimesTask, moviesTask);

        return Json(new { 
            success = true, 
            date = selectedDate.ToString("yyyy-MM-dd"), 
            cinemas = await cinemasTask, 
            screens = await screensTask, 
            showtimes = await showtimesTask,
            movies = await moviesTask
        });
    }
}

/// <summary>
/// ViewModel for Showtime Calendar layout.
/// </summary>
[EnableRateLimiting("fixed")]
public class ShowTimeCalendarViewModel
{
    public DateOnly SelectedDate { get; set; }
    public IReadOnlyList<CinemaDto> Cinemas { get; set; } = [];
    public IReadOnlyList<ScreenDto> Screens { get; set; } = [];
    public IReadOnlyList<ShowTimeDto> ShowTimes { get; set; } = [];
    public IReadOnlyList<MovieDto> Movies { get; set; } = [];
}


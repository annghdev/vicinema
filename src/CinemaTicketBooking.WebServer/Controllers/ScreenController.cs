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
/// Handles screen management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class ScreenController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a paged list of cinema screens.
    /// </summary>
    [Authorize(Policy = Permissions.ScreensView)]
    public async Task<IActionResult> Index(Guid? cinemaId, int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
    {
        ViewData["Title"] = "Quản lý phòng chiếu";

        var query = new GetPagedScreensQuery
        {
            CinemaId = cinemaId,
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm
        };

        var result = await bus.InvokeAsync<PagedResult<ScreenDto>>(query);
        
        var cinemaDropdown = await bus.InvokeAsync<IReadOnlyList<CinemaDropdownDto>>(new GetCinemaDropdownQuery());
        ViewBag.Cinemas = cinemaDropdown;
        ViewBag.SelectedCinemaId = cinemaId;

        return View(result);
    }

    /// <summary>
    /// Creates a new screen via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ScreensManage)]
    public async Task<IActionResult> Create([FromBody] AddScreenCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Thêm phòng chiếu thành công!", id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Updates screen basic info via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ScreensManage)]
    public async Task<IActionResult> Update([FromBody] UpdateScreenBasicInfoCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật phòng chiếu thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Deactivates a screen via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ScreensManage)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new DeactivateScreenCommand { Id = id });
            return Json(new { success = true, message = "Ngưng hoạt động phòng chiếu!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Activates a screen via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ScreensManage)]
    public async Task<IActionResult> Activate(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new ActivateScreenCommand { Id = id });
            return Json(new { success = true, message = "Kích hoạt phòng chiếu thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


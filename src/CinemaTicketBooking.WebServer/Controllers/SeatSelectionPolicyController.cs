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
/// Handles seat selection policy management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class SeatSelectionPolicyController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a list of all seat selection policies.
    /// </summary>
    [Authorize(Policy = Permissions.SeatSelectionPoliciesView)]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cấu hình chọn ghế";
        var items = await bus.InvokeAsync<IReadOnlyList<SeatSelectionPolicyDto>>(new GetSeatSelectionPoliciesQuery());
        return View(items);
    }

    /// <summary>
    /// Adds a new seat selection policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SeatSelectionPoliciesManage)]
    public async Task<IActionResult> Create([FromBody] AddSeatSelectionPolicyCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Đã thêm cấu hình chọn ghế thành công!", id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing seat selection policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SeatSelectionPoliciesManage)]
    public async Task<IActionResult> Update([FromBody] UpdateSeatSelectionPolicyCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Đã cập nhật cấu hình chọn ghế thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Sets activation status for a seat selection policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SeatSelectionPoliciesManage)]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleStatusRequest request)
    {
        try
        {
            if (request.IsActive)
            {
                await bus.InvokeAsync(new ActiveSeatSelectionPolicyCommand { Id = request.Id });
            }
            else
            {
                await bus.InvokeAsync(new DeactiveSeatSelectionPolicyCommand { Id = request.Id });
            }
            
            var status = request.IsActive ? "kích hoạt" : "tạm ngưng";
            return Json(new { success = true, message = $"Đã {status} cấu hình thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record ToggleStatusRequest(Guid Id, bool IsActive);
}


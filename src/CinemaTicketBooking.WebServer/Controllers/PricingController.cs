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
/// Handles pricing policy management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class PricingController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a paged list of pricing policies.
    /// </summary>
    [Authorize(Policy = Permissions.PricingPoliciesView)]
    public async Task<IActionResult> Index(
        int pageNumber = 1, 
        int pageSize = 10, 
        Guid? cinemaId = null,
        ScreenType? screenType = null,
        SeatType? seatType = null)
    {
        ViewData["Title"] = "Quản lý giá vé";

        // 1. Fetch paged results
        var query = new GetPagedPricingPoliciesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            CinemaId = cinemaId,
            ScreenType = screenType,
            SeatType = seatType
        };

        var result = await bus.InvokeAsync<PagedResult<PricingPolicyDto>>(query);

        // 2. Fetch Cinemas for filter dropdown
        var cinemas = await bus.InvokeAsync<IReadOnlyList<CinemaDropdownDto>>(new GetCinemaDropdownQuery());
        ViewBag.Cinemas = cinemas;

        return View(result);
    }

    // =============================================
    // CRUD Operations (AJAX)
    // =============================================

    /// <summary>
    /// Creates a new pricing policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PricingPoliciesManage)]
    public async Task<IActionResult> Create([FromBody] AddPricingPolicyCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Thêm chính sách giá mới thành công!", id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Updates pricing policy info via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PricingPoliciesManage)]
    public async Task<IActionResult> Update([FromBody] UpdatePricingPolicyInfoCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật chính sách giá thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Sets activation status for a pricing policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PricingPoliciesManage)]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleActivationRequest request)
    {
        try
        {
            if (request.IsActive)
            {
                await bus.InvokeAsync(new SetPricingPolicyActiveCommand { Id = request.Id });
            }
            else
            {
                await bus.InvokeAsync(new SetPricingPolicyInactiveCommand { Id = request.Id });
            }
            
            var status = request.IsActive ? "kích hoạt" : "hủy kích hoạt";
            return Json(new { success = true, message = $"Đã {status} chính sách giá thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record ToggleActivationRequest(Guid Id, bool IsActive);

    /// <summary>
    /// Deletes a pricing policy via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PricingPoliciesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new DeletePricingPolicyCommand { Id = id });
            return Json(new { success = true, message = "Xóa chính sách giá thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


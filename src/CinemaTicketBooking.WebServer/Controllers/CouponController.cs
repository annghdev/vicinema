using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features.Coupons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Admin controller for coupon template management.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class CouponController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays all coupon templates.
    /// </summary>
    [Authorize(Policy = Permissions.CouponsView)]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý mã giảm giá";

        var templates = await bus.InvokeAsync<IReadOnlyList<CouponTemplateDto>>(
            new GetAllCouponTemplatesQuery { CorrelationId = string.Empty });

        return View(templates);
    }

    /// <summary>
    /// Shows the create coupon template form.
    /// </summary>
    [Authorize(Policy = Permissions.CouponsManage)]
    public IActionResult Create()
    {
        ViewData["Title"] = "Tạo mã giảm giá";
        return View(new CreateCouponTemplateCommand
        {
            DurationDays = 30,
            IsActive = true,
            MaxUsagePerUser = 1
        });
    }

    /// <summary>
    /// Creates a new coupon template.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.CouponsManage)]
    public async Task<IActionResult> Create(CreateCouponTemplateCommand command)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Tạo mã giảm giá";
            return View(command);
        }

        try
        {
            await bus.InvokeAsync(command);
            TempData["SuccessMessage"] = "Tạo mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "Tạo mã giảm giá";
            return View(command);
        }
    }

    /// <summary>
    /// Toggles a coupon template's active status via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.CouponsManage)]
    public async Task<IActionResult> Toggle([FromBody] ToggleRequest request)
    {
        try
        {
            await bus.InvokeAsync(new ToggleCouponTemplateCommand { TemplateId = request.Id });
            return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record ToggleRequest(Guid Id);
}
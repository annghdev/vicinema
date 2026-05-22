using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Admin controller for loyalty tier configuration management.
/// Only supports viewing and updating — tiers are seeded once at startup.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class LoyaltyController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays all loyalty tier configurations.
    /// </summary>
    [Authorize(Policy = Permissions.LoyaltyView)]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Quản lý hạng hội viên";

        var tiers = await bus.InvokeAsync<IReadOnlyList<LoyaltyTierDto>>(
            new GetAllLoyaltyTiersQuery());

        return View(tiers);
    }

    /// <summary>
    /// Updates a loyalty tier configuration via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.LoyaltyManage)]
    public async Task<IActionResult> Update([FromBody] UpdateLoyaltyTierConfigurationCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật cấu hình hạng hội viên thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}

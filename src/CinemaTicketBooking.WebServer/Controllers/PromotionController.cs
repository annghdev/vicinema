using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers;

[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class PromotionController(IMessageBus bus) : Controller
{
    [HttpGet]
    [Authorize(Policy = Permissions.PromotionsView)]
    public IActionResult Index()
    {
        ViewData["Title"] = "Quản lý chương trình khuyến mãi";
        return View();
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PromotionsView)]
    public async Task<IActionResult> GetList()
    {
        var result = await bus.InvokeAsync<PagedResult<PromotionProgramDto>>(
            new GetPromotionProgramsQuery());
        return Json(new { data = result.Items, total = result.TotalItems });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PromotionsCreate)]
    public async Task<IActionResult> Create([FromBody] CreatePromotionProgramCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Tạo chương trình khuyến mãi thành công!", data = new { id } });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PromotionsEdit)]
    public async Task<IActionResult> Edit([FromBody] UpdatePromotionProgramCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật chương trình khuyến mãi thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PromotionsEdit)]
    public async Task<IActionResult> Toggle([FromBody] TogglePromotionProgramCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PromotionsDelete)]
    public async Task<IActionResult> Delete([FromBody] DeletePromotionProgramCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Xóa chương trình khuyến mãi thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PromotionsView)]
    public async Task<IActionResult> Detail(Guid id)
    {
        var detail = await bus.InvokeAsync<PromotionProgramDetailDto?>(
            new GetPromotionProgramByIdQuery { Id = id });
        return detail is null ? NotFound() : Json(detail);
    }
}

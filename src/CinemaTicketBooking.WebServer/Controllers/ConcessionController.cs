using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Common.Auth;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.WebServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Wolverine;

namespace CinemaTicketBooking.WebServer.Controllers;

/// <summary>
/// Handles concession management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class ConcessionController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a paged list of concessions.
    /// </summary>
    [Authorize(Policy = Permissions.ConcessionsView)]
    public async Task<IActionResult> Index(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? searchTerm = null,
        bool? isAvailable = null)
    {
        ViewData["Title"] = "Quản lý bắp nước";

        var query = new GetPagedConcessionsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsAvailable = isAvailable,
            SortBy = "createdAt",
            SortDirection = "desc"
        };

        var result = await bus.InvokeAsync<PagedResult<ConcessionDto>>(query);
        return View(result);
    }

    /// <summary>
    /// Adds a new concession via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ConcessionsManage)]
    public async Task<IActionResult> Create([FromBody] AddConcessionCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Đã thêm bắp nước thành công!", id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Updates an existing concession via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ConcessionsManage)]
    public async Task<IActionResult> Update([FromBody] UpdateConcessionInfoCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Đã cập nhật bắp nước thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Sets availability status for a concession via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ConcessionsManage)]
    public async Task<IActionResult> ToggleStatus([FromBody] ToggleAvailabilityRequest request)
    {
        try
        {
            if (request.IsAvailable)
            {
                await bus.InvokeAsync(new SetConcessionAvailableCommand { Id = request.Id });
            }
            else
            {
                await bus.InvokeAsync(new SetConcessionUnavailableCommand { Id = request.Id });
            }
            
            var status = request.IsAvailable ? "mở bán" : "tạm ngưng";
            return Json(new { success = true, message = $"Đã {status} bắp nước thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    public record ToggleAvailabilityRequest(Guid Id, bool IsAvailable);

    /// <summary>
    /// Deletes a concession via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ConcessionsManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new DeleteConcessionCommand { Id = id });
            return Json(new { success = true, message = "Đã xóa bắp nước thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Uploads an image for a concession item via multipart form.
    /// Returns the URL for the UI to include in create/update payloads.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ConcessionsManage)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        var error = FileUploadValidator.ValidateImageFile(file);
        if (error is not null)
        {
            return Json(new { success = false, message = error });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var command = new UploadImageCommand
            {
                Group = "concessions",
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length
            };

            var url = await bus.InvokeAsync<string>(command);
            return Json(new { success = true, message = "Upload ảnh thành công!", url });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


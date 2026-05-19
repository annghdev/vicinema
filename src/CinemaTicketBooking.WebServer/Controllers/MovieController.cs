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
/// Handles movie management server-side rendered pages.
/// </summary>
[Authorize(AuthenticationSchemes = "Identity.Application")]
[EnableRateLimiting("fixed")]
public class MovieController(IMessageBus bus) : Controller
{
    /// <summary>
    /// Displays a paged list of movies.
    /// </summary>
    [Authorize(Policy = Permissions.MoviesView)]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, string? status = null, string? genre = null)
    {
        ViewData["Title"] = "Movie Library";
        
        MovieStatus? movieStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<MovieStatus>(status, true, out var parsedStatus))
        {
            movieStatus = parsedStatus;
        }

        MovieGenre? movieGenre = null;
        if (!string.IsNullOrEmpty(genre) && Enum.TryParse<MovieGenre>(genre, true, out var parsedGenre))
        {
            movieGenre = parsedGenre;
        }

        var query = new GetPagedMoviesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            Status = movieStatus,
            Genre = movieGenre
        };

        var result = await bus.InvokeAsync<PagedResult<MovieDto>>(query);

        return View(result);
    }

    /// <summary>
    /// Creates a new movie via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.MoviesManage)]
    public async Task<IActionResult> Create([FromBody] CreateMovieCommand command)
    {
        try
        {
            var id = await bus.InvokeAsync<Guid>(command);
            return Json(new { success = true, message = "Thêm phim mới thành công!", id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Updates movie basic info via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.MoviesManage)]
    public async Task<IActionResult> Update([FromBody] UpdateMovieBasicInfoCommand command)
    {
        try
        {
            await bus.InvokeAsync(command);
            return Json(new { success = true, message = "Cập nhật phim thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a movie via AJAX.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.MoviesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await bus.InvokeAsync(new DeleteMovieCommand { Id = id });
            return Json(new { success = true, message = "Xóa phim thành công!" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Uploads a thumbnail image for a movie via multipart form.
    /// Returns the URL for the UI to include in create/update payloads.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = Permissions.MoviesManage)]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadThumbnail(IFormFile file)
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
                Group = "movies",
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length
            };

            var url = await bus.InvokeAsync<string>(command);
            return Json(new { success = true, message = "Upload thumbnail thành công!", url });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}


using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class ScreenEndpoints
{
    public static void MapScreenEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/screens")
            .RequireRateLimiting("fixed")
            .WithTags("Screens")
            .AllowAnonymous();

        group.MapGet("/", GetScreensAsync);
        group.MapGet("/paged", GetPagedScreensAsync);
        group.MapGet("/dropdown", GetScreenDropdownAsync);
        group.MapGet("/{id:guid}", GetScreenByIdAsync);

        group.MapPost("/", AddScreenAsync);
        group.MapPut("/{id:guid}", UpdateScreenAsync);
        group.MapPost("/{id:guid}/activate", ActivateScreenAsync);
        group.MapPost("/{id:guid}/deactivate", DeactivateScreenAsync);
        group.MapPost("/{screenId:guid}/seats/{seatId:guid}/activate", ActivateScreenSeatAsync);
        group.MapPost("/{screenId:guid}/seats/{seatId:guid}/deactivate", DeactivateScreenSeatAsync);
    }

    private static async Task<IResult> GetScreensAsync(
        Guid? cinemaId,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<ScreenDto>>(
            new GetScreensQuery { CinemaId = cinemaId },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPagedScreensAsync(
        [AsParameters] GetPagedScreensRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PagedResult<ScreenDto>>(
            new GetPagedScreensQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                CinemaId = request.CinemaId,
                SearchTerm = request.SearchTerm,
                IsActive = request.IsActive,
                Type = request.Type,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetScreenDropdownAsync(
        [AsParameters] GetScreenDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<ScreenDropdownDto>>(
            new GetScreenDropdownQuery
            {
                CinemaId = request.CinemaId,
                SearchTerm = request.SearchTerm,
                OnlyActive = request.OnlyActive,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetScreenByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<ScreenDetailDto?>(new GetScreenByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> AddScreenAsync([FromBody] AddScreenCommand command, IMessageBus bus, CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdateScreenAsync(
        Guid id,
        [FromBody] UpdateScreenBasicInfoCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        command.Id = id;
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateScreenAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new ActivateScreenCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateScreenAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeactivateScreenCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateScreenSeatAsync(Guid screenId, Guid seatId, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new ActivateScreenSeatCommand { ScreenId = screenId, SeatId = seatId }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateScreenSeatAsync(Guid screenId, Guid seatId, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeactivateScreenSeatCommand { ScreenId = screenId, SeatId = seatId }, ct);
        return Results.NoContent();
    }
}

public sealed class GetPagedScreensRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CinemaId { get; init; }
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public ScreenType? Type { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed class GetScreenDropdownRequest
{
    public Guid? CinemaId { get; init; }
    public string? SearchTerm { get; init; }
    public bool OnlyActive { get; init; } = true;
    public int MaxItems { get; init; } = 100;
}


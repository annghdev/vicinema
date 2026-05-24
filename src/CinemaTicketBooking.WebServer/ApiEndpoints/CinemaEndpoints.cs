using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class CinemaEndpoints
{
    public static void MapCinemaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/cinemas")
            .RequireRateLimiting("fixed")
            .WithTags("Cinemas")
            .AllowAnonymous();

        group.MapGet("/", GetCinemasAsync);
        group.MapGet("/paged", GetPagedCinemasAsync);
        group.MapGet("/dropdown", GetCinemaDropdownAsync);
        group.MapGet("/{id:guid}", GetCinemaByIdAsync);

        group.MapPost("/", CreateCinemaAsync);
        group.MapPut("/{id:guid}", UpdateCinemaAsync);
        group.MapDelete("/{id:guid}", DeleteCinemaAsync);
    }

    private static async Task<IResult> GetCinemasAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<CinemaDto>>(new GetCinemasQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPagedCinemasAsync(
        [AsParameters] GetPagedCinemasRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PagedResult<CinemaDto>>(
            new GetPagedCinemasQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                IsActive = request.IsActive,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCinemaDropdownAsync(
        [AsParameters] GetCinemaDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<CinemaDropdownDto>>(
            new GetCinemaDropdownQuery
            {
                SearchTerm = request.SearchTerm,
                OnlyActive = request.OnlyActive,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetCinemaByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<CinemaDto?>(new GetCinemaByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateCinemaAsync([FromBody] CreateCinemaCommand command, IMessageBus bus, CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdateCinemaAsync(
        Guid id,
        [FromBody] UpdateCinemaBasicInfoCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        command.Id = id;
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCinemaAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeleteCinemaCommand { Id = id }, ct);
        return Results.NoContent();
    }
}

public sealed class GetPagedCinemasRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public bool? IsActive { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed class GetCinemaDropdownRequest
{
    public string? SearchTerm { get; init; }
    public bool OnlyActive { get; init; } = true;
    public int MaxItems { get; init; } = 100;
}


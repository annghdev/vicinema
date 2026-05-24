using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

/// <summary>
/// Defines endpoints for concession management.
/// </summary>
public static class ConcessionEndpoints
{
    /// <summary>
    /// Maps concession endpoints to the web application.
    /// </summary>
    public static void MapConcessionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/concessions")
            .RequireRateLimiting("fixed")
            .WithTags("Concessions")
            .AllowAnonymous();

        group.MapGet("/", GetConcessionsAsync);
        group.MapGet("/paged", GetPagedConcessionsAsync);
        group.MapGet("/dropdown", GetConcessionDropdownAsync);
        group.MapGet("/{id:guid}", GetConcessionByIdAsync);

        group.MapPost("/", CreateConcessionAsync);
        group.MapPut("/{id:guid}", UpdateConcessionAsync);
        group.MapPut("/{id:guid}/available", SetAvailableAsync);
        group.MapPut("/{id:guid}/unavailable", SetUnavailableAsync);
        group.MapDelete("/{id:guid}", DeleteConcessionAsync);
    }

    // =============================================
    // Queries
    // =============================================

    private static async Task<IResult> GetConcessionsAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<ConcessionDto>>(new GetConcessionsQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPagedConcessionsAsync(
        [AsParameters] GetPagedConcessionsRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PagedResult<ConcessionDto>>(
            new GetPagedConcessionsQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                IsAvailable = request.OnlyAvailable,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetConcessionDropdownAsync(
        [AsParameters] GetConcessionDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<ConcessionDropdownDto>>(
            new GetConcessionDropdownQuery
            {
                SearchTerm = request.SearchTerm,
                OnlyAvailable = request.OnlyAvailable ?? true,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetConcessionByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<ConcessionDto?>(new GetConcessionByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    // =============================================
    // Commands
    // =============================================

    private static async Task<IResult> CreateConcessionAsync(
        [FromBody] AddConcessionCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdateConcessionAsync(
        Guid id,
        [FromBody] UpdateConcessionInfoCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        command.Id = id;
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetAvailableAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new SetConcessionAvailableCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetUnavailableAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new SetConcessionUnavailableCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteConcessionAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeleteConcessionCommand { Id = id }, ct);
        return Results.NoContent();
    }
}

/// <summary>
/// Parameters for paged concession requests.
/// </summary>
public sealed class GetPagedConcessionsRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public bool? OnlyAvailable { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

/// <summary>
/// Parameters for concession dropdown requests.
/// </summary>
public sealed class GetConcessionDropdownRequest
{
    public string? SearchTerm { get; init; }
    public bool? OnlyAvailable { get; init; }
    public int MaxItems { get; init; } = 100;
}

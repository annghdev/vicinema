using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class MovieEndpoints
{
    public static void MapMovieEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/movies")
            .RequireRateLimiting("fixed")
            .WithTags("Movies")
            .AllowAnonymous();

        group.MapGet("/", GetMoviesAsync);
        group.MapGet("/paged", GetPagedMoviesAsync);
        group.MapGet("/dropdown", GetMovieDropdownAsync);
        group.MapGet("/upcoming-now-showing", GetUpcomingAndNowShowingMoviesAsync);
        group.MapGet("/upcoming-now-showing/dropdown", GetUpcomingAndNowShowingMovieDropdownAsync);
        group.MapGet("/{id:guid}", GetMovieByIdAsync);

        group.MapPost("/", CreateMovieAsync);
        group.MapPut("/{id:guid}", UpdateMovieAsync);
        group.MapDelete("/{id:guid}", DeleteMovieAsync);
    }

    private static async Task<IResult> GetMoviesAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<MovieDto>>(new GetMoviesQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPagedMoviesAsync(
        [AsParameters] GetPagedMoviesRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PagedResult<MovieDto>>(
            new GetPagedMoviesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                SearchTerm = request.SearchTerm,
                Status = request.Status,
                Genre = request.Genre,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMovieDropdownAsync(
        [AsParameters] GetMovieDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<MovieDropdownDto>>(
            new GetMovieDropdownQuery
            {
                SearchTerm = request.SearchTerm,
                Status = request.Status,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUpcomingAndNowShowingMoviesAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<MovieDto>>(new GetUpcomingAndNowShowingMoviesQuery(), ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetUpcomingAndNowShowingMovieDropdownAsync(
        [AsParameters] GetUpcomingAndNowShowingMovieDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<MovieDropdownDto>>(
            new GetUpcomingAndNowShowingMovieDropdownQuery
            {
                SearchTerm = request.SearchTerm,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetMovieByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<MovieDto?>(new GetMovieByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateMovieAsync([FromBody] CreateMovieCommand command, IMessageBus bus, CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdateMovieAsync(
        Guid id,
        [FromBody] UpdateMovieBasicInfoCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        command.Id = id;
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteMovieAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeleteMovieCommand { Id = id }, ct);
        return Results.NoContent();
    }
}

public sealed class GetPagedMoviesRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public MovieStatus? Status { get; init; }
    public MovieGenre? Genre { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed class GetMovieDropdownRequest
{
    public string? SearchTerm { get; init; }
    public MovieStatus? Status { get; init; }
    public int MaxItems { get; init; } = 100;
}

public sealed class GetUpcomingAndNowShowingMovieDropdownRequest
{
    public string? SearchTerm { get; init; }
    public int MaxItems { get; init; } = 100;
}


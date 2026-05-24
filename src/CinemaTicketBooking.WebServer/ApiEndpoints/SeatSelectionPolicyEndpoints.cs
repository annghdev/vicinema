using CinemaTicketBooking.Application.Features;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class SeatSelectionPolicyEndpoints
{
    public static void MapSeatSelectionPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/seat-selection-policies")
            .RequireRateLimiting("fixed")
            .WithTags("SeatSelectionPolicies")
            .AllowAnonymous();

        group.MapGet("/", GetSeatSelectionPoliciesAsync);
        group.MapGet("/{id:guid}", GetSeatSelectionPolicyByIdAsync);

        group.MapPost("/", CreateSeatSelectionPolicyAsync);
        group.MapPut("/{id:guid}", UpdateSeatSelectionPolicyAsync);
        group.MapPatch("/{id:guid}/active", ActivateSeatSelectionPolicyAsync);
        group.MapPatch("/{id:guid}/deactive", DeactivateSeatSelectionPolicyAsync);
    }

    private static async Task<IResult> GetSeatSelectionPoliciesAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<SeatSelectionPolicyDto>>(new GetSeatSelectionPoliciesQuery(), ct);
        return Results.Ok(result);
    }


    private static async Task<IResult> GetSeatSelectionPolicyByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<SeatSelectionPolicyDto?>(new GetSeatSelectionPolicyByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreateSeatSelectionPolicyAsync(
        [FromBody] AddSeatSelectionPolicyCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdateSeatSelectionPolicyAsync(
        Guid id,
        [FromBody] UpdateSeatSelectionPolicyCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        command.Id = id;
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateSeatSelectionPolicyAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new ActiveSeatSelectionPolicyCommand { Id = id}, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeactivateSeatSelectionPolicyAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new DeactiveSeatSelectionPolicyCommand { Id = id }, ct);
        return Results.NoContent();
    }
}


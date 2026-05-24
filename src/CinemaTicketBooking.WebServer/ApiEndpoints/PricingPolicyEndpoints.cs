using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Domain;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class PricingPolicyEndpoints
{
    public static void MapPricingPolicyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/pricing-policies")
            .RequireRateLimiting("fixed")
            .WithTags("PricingPolicies")
            .AllowAnonymous();

        group.MapGet("/", GetPricingPoliciesAsync);
        group.MapGet("/paged", GetPagedPricingPoliciesAsync);
        group.MapGet("/dropdown", GetPricingPolicyDropdownAsync);
        group.MapGet("/{id:guid}", GetPricingPolicyByIdAsync);

        group.MapPost("/", CreatePricingPolicyAsync);
        group.MapPut("/{id:guid}", UpdatePricingPolicyAsync);
        group.MapPatch("/{id:guid}/active", SetPricingPolicyActiveAsync);
        group.MapPatch("/{id:guid}/inactive", SetPricingPolicyInactiveAsync);
        group.MapDelete("/{id:guid}", DeletePricingPolicyAsync);
    }

    private static async Task<IResult> GetPricingPoliciesAsync(
        [AsParameters] GetPricingPoliciesRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<PricingPolicyDto>>(
            new GetPricingPoliciesQuery
            {
                CinemaId = request.CinemaId
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPagedPricingPoliciesAsync(
        [AsParameters] GetPagedPricingPoliciesRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PagedResult<PricingPolicyDto>>(
            new GetPagedPricingPoliciesQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                CinemaId = request.CinemaId,
                ScreenType = request.ScreenType,
                SeatType = request.SeatType,
                IsActive = request.IsActive,
                SortBy = request.SortBy,
                SortDirection = request.SortDirection
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPricingPolicyDropdownAsync(
        [AsParameters] GetPricingPolicyDropdownRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<PricingPolicyDropdownDto>>(
            new GetPricingPolicyDropdownQuery
            {
                CinemaId = request.CinemaId,
                OnlyActive = request.OnlyActive,
                MaxItems = request.MaxItems
            },
            ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPricingPolicyByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PricingPolicyDto?>(new GetPricingPolicyByIdQuery { Id = id }, ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> CreatePricingPolicyAsync(
        [FromBody] AddPricingPolicyCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        var id = await bus.InvokeAsync<Guid>(command, ct);
        return Results.Ok(new { Id = id });
    }

    private static async Task<IResult> UpdatePricingPolicyAsync(
        Guid id,
        [FromBody] UpdatePricingPolicyInfoCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(command, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetPricingPolicyActiveAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new SetPricingPolicyActiveCommand { Id = id }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SetPricingPolicyInactiveAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken ct)
    {

        await bus.InvokeAsync(new SetPricingPolicyInactiveCommand { Id = id }, ct);
        return Results.NoContent();

    }

    private static async Task<IResult> DeletePricingPolicyAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        await bus.InvokeAsync(new DeletePricingPolicyCommand { Id = id }, ct);
        return Results.NoContent();
    }
}

public sealed class GetPricingPoliciesRequest
{
    public Guid? CinemaId { get; init; }
}

public sealed class GetPagedPricingPoliciesRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public Guid? CinemaId { get; init; }
    public ScreenType? ScreenType { get; init; }
    public SeatType? SeatType { get; init; }
    public bool? IsActive { get; init; }
    public string SortBy { get; init; } = "createdAt";
    public string SortDirection { get; init; } = "desc";
}

public sealed class GetPricingPolicyDropdownRequest
{
    public Guid? CinemaId { get; init; }
    public bool OnlyActive { get; init; } = true;
    public int MaxItems { get; init; } = 100;
}


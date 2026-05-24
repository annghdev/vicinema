using System.Security.Claims;
using CinemaTicketBooking.Application.Features.Loyalty;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

/// <summary>
/// Customer loyalty API endpoints.
/// </summary>
public static class LoyaltyEndpoints
{
    /// <summary>
    /// Maps loyalty routes.
    /// </summary>
    public static void MapLoyaltyEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/loyalty")
            .RequireRateLimiting("fixed").WithTags("Loyalty");

        group.MapGet("/me", GetMyLoyalty)
            .RequireAuthorization();

        group.MapGet("/tiers", GetActiveTiers);
    }

    /// <summary>
    /// Returns the current authenticated customer's loyalty info.
    /// </summary>
    private static async Task<IResult> GetMyLoyalty(
        IMessageBus bus,
        HttpContext http,
        UserManager<Account> userManager,
        CancellationToken ct)
    {
        // 1. Get account ID from identity claims
        var accountIdRaw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdRaw, out var accountId))
            return Results.Unauthorized();

        // 2. Load account to get linked CustomerId
        var account = await userManager.FindByIdAsync(accountId.ToString());
        if (account?.CustomerId is not { } customerId)
            return Results.NotFound();

        // 3. Fetch loyalty info
        var dto = await bus.InvokeAsync<CustomerLoyaltyDto?>(
            new GetCustomerLoyaltyQuery { CustomerId = customerId },
            ct);

        return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    /// <summary>
    /// Returns all active loyalty tiers with benefits (public).
    /// </summary>
    private static async Task<IResult> GetActiveTiers(
        IMessageBus bus,
        CancellationToken ct)
    {
        var tiers = await bus.InvokeAsync<IReadOnlyList<LoyaltyTierDto>>(
            new GetActiveLoyaltyTiersQuery(),
            ct);

        return Results.Ok(tiers);
    }
}

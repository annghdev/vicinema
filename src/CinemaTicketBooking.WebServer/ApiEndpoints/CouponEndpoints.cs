using System.Security.Claims;
using CinemaTicketBooking.Application.Features.Coupons;
using CinemaTicketBooking.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

/// <summary>
/// Customer and admin coupon API endpoints.
/// </summary>
public static class CouponEndpoints
{
    /// <summary>
    /// Maps coupon routes.
    /// </summary>
    public static void MapCouponEndpoints(this WebApplication app)
    {
        var customerGroup = app.MapGroup("/api/coupons")
            .RequireRateLimiting("fixed").WithTags("Coupons");

        customerGroup.MapGet("/my-coupons", GetMyCoupons)
            .RequireAuthorization();

        customerGroup.MapGet("/public", GetPublicTemplates);

        customerGroup.MapPost("/verify", VerifyCoupon);
    }

    /// <summary>
    /// Returns the current authenticated customer's available coupons.
    /// </summary>
    private static async Task<IResult> GetMyCoupons(
        IMessageBus bus,
        HttpContext http,
        UserManager<Account> userManager,
        CancellationToken ct)
    {
        var accountIdRaw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(accountIdRaw, out var accountId))
            return Results.Unauthorized();

        var account = await userManager.FindByIdAsync(accountId.ToString());
        if (account?.CustomerId is not { } customerId)
            return Results.NotFound();

        var coupons = await bus.InvokeAsync<IReadOnlyList<CustomerCouponDto>>(
            new GetCustomerCouponsQuery { CustomerId = customerId, CorrelationId = string.Empty },
            ct);

        return Results.Ok(coupons);
    }

    /// <summary>
    /// Returns all active public coupon templates.
    /// </summary>
    private static async Task<IResult> GetPublicTemplates(
        IMessageBus bus,
        CancellationToken ct)
    {
        var templates = await bus.InvokeAsync<IReadOnlyList<CouponTemplateDto>>(
            new GetPublicCouponTemplatesQuery { CorrelationId = string.Empty },
            ct);

        return Results.Ok(templates);
    }

    /// <summary>
    /// Verifies a coupon code and returns discount info.
    /// </summary>
    private static async Task<IResult> VerifyCoupon(
        [FromBody] VerifyCouponRequest request,
        IMessageBus bus,
        HttpContext http,
        UserManager<Account> userManager,
        CancellationToken ct)
    {
        Guid? customerId = null;

        var accountIdRaw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(accountIdRaw, out var accountId))
        {
            var account = await userManager.FindByIdAsync(accountId.ToString());
            customerId = account?.CustomerId;
        }

        var result = await bus.InvokeAsync<CouponValidationResultDto>(
            new VerifyCouponQuery
            {
                CouponCode = request.CouponCode,
                CustomerId = customerId
            },
            ct);

        return Results.Ok(result);
    }

    public record VerifyCouponRequest(string CouponCode);
}
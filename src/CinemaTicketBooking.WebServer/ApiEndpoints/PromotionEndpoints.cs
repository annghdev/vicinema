using CinemaTicketBooking.Application.Features.Promotions;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/promotions")
            .RequireRateLimiting("fixed")
            .WithTags("Promotions");

        group.MapGet("/active", GetActivePromotionsAsync);

        group.MapGet("/{id:guid}", GetPromotionByIdAsync);
    }

    private static async Task<IResult> GetActivePromotionsAsync(IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<IReadOnlyList<PromotionProgramDto>>(
            new GetActivePromotionsQuery(), ct);
        return Results.Ok(new { promotions = result, timestamp = DateTimeOffset.UtcNow });
    }

    private static async Task<IResult> GetPromotionByIdAsync(Guid id, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.InvokeAsync<PromotionProgramDetailDto?>(
            new GetPromotionProgramByIdQuery { Id = id }, ct);
        return result is not null ? Results.Ok(result) : Results.NotFound();
    }
}

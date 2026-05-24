using CinemaTicketBooking.Application.Features;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class SlideEndpoints
{
    public static void MapSlideEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/slides")
            .RequireRateLimiting("fixed")
            .WithTags("Slides")
            .AllowAnonymous();

        group.MapGet("/", async (IMessageBus bus, CancellationToken ct) =>
        {
            var slides = await bus.InvokeAsync<IReadOnlyList<SlideDto>>(new GetActiveSlidesQuery(), ct);
            return Results.Ok(slides);
        });
    }
}



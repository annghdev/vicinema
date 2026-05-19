using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.WebServer.Middlewares;

/// <summary>
/// HTTP middleware that assigns a Correlation ID to every inbound request.
/// Priority: X-Correlation-Id request header → generate new Guid v7.
/// Stores the ID in <see cref="ICorrelationIdAccessor"/> (scoped) and echoes it
/// back to the caller via the X-Correlation-Id response header.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    internal const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Resolves or generates the correlation ID, then forwards the request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor accessor)
    {
        // 1. Prefer the ID forwarded by the client / gateway; otherwise create a fresh one.
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.CreateVersion7().ToString();

        // 2. Populate the scoped accessor so downstream code (filters, handlers) can read it.
        accessor.Set(correlationId);

        // 3. Echo the ID back to the caller so the client can correlate its own logs.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}

using CinemaTicketBooking.Application.Abstractions;
using Wolverine;

namespace CinemaTicketBooking.Application.Common.PipelineMiddlewares;

/// <summary>
/// Wolverine middleware: automatically assigns Correlation ID from the scoped accessor
/// or generates a new Guid Version 7 if running outside of an HTTP request context.
/// </summary>
public static class CorrelationIdWolverineMiddleware
{
    /// <summary>
    /// Executes before handling any message. Inspects the envelope message, and if it implements
    /// <see cref="IRequest"/>, ensures CorrelationId tracing is intact.
    /// </summary>
    /// <param name="envelope">The wolverine envelope containing the message context.</param>
    /// <param name="accessor">The scoped correlation ID accessor.</param>
    public static void Before(Envelope envelope, ICorrelationIdAccessor accessor)
    {
        if (envelope.Message is IRequest request && string.IsNullOrEmpty(request.CorrelationId))
        {
            request.CorrelationId = !string.IsNullOrEmpty(accessor.CorrelationId)
                ? accessor.CorrelationId
                : Guid.CreateVersion7().ToString();
        }
    }
}

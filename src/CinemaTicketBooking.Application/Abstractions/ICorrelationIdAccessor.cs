namespace CinemaTicketBooking.Application.Abstractions;

/// <summary>
/// Provides read/write access to the correlation ID for the current request scope.
/// Set once by HTTP middleware; consumed by endpoint filters and application pipeline.
/// </summary>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the correlation ID assigned to the current request.
    /// Returns <see cref="string.Empty"/> when called outside an HTTP request scope
    /// (e.g. background services that set their own IDs directly on the message).
    /// </summary>
    string CorrelationId { get; }

    /// <summary>
    /// Sets the correlation ID for the current request scope.
    /// Should be called only once, by <c>CorrelationIdMiddleware</c>.
    /// </summary>
    void Set(string correlationId);
}

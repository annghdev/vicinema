using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.WebServer.Middlewares;

/// <summary>
/// Scoped implementation of <see cref="ICorrelationIdAccessor"/>.
/// A single instance per HTTP request is stored in the DI scope;
/// <see cref="CorrelationIdMiddleware"/> calls <see cref="Set"/> exactly once.
/// </summary>
public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    private string _correlationId = string.Empty;

    /// <inheritdoc/>
    public string CorrelationId => _correlationId;

    /// <inheritdoc/>
    public void Set(string correlationId) =>
        _correlationId = correlationId;
}

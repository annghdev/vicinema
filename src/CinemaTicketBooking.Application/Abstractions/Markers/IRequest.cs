namespace CinemaTicketBooking.Application;

public interface IRequest
{
    string CorrelationId { get; set; }
}

public interface ICommand : IRequest;
public interface IQuery : IRequest;

/// <summary>
/// Query with compile-time response type (matches handler <c>Task&lt;TResponse&gt;</c> / <c>Task&lt;TResponse?&gt;</c>).
/// </summary>
public interface IQuery<out TResponse> : IQuery;

/// <summary>
/// Marker for queries that may use <see cref="Common.PipelineMiddlewares.CachingMiddleware"/> (generic overload).
/// </summary>
public interface ICachableQuery : IQuery, ICachable;

/// <summary>
/// Cachable query with response type for cache serialization (no reflection on handler metadata).
/// </summary>
/// <typeparam name="TResponse">CLR type of the handler return value (same as <c>InvokeAsync&lt;TResponse&gt;</c>).</typeparam>
public interface ICachableQuery<out TResponse> : ICachableQuery, IQuery<TResponse>;

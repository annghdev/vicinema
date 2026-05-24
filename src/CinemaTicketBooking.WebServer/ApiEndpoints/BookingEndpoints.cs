using CinemaTicketBooking.Application;
using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Application.Features.Bookings.Commands;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

/// <summary>
/// Customer booking APIs.
/// </summary>
public static class BookingEndpoints
{
    /// <summary>
    /// Maps booking routes.
    /// </summary>
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/bookings")
            .RequireRateLimiting("fixed").WithTags("Bookings");
        group.MapGet("/{bookingId:guid}", GetBookingById);
        group.MapGet("/history/{customerId:guid}", GetBookingsByCustomerId);
        group.MapPost("/preview-pricing", PreviewPricing);
        group.MapPost("/", CreateBooking);
        group.MapPut("/{id:guid}/checkin", CheckInBooking);
        group.MapPut("/{id:guid}/cancel", CancelBooking);
        group.MapPost("/{id:guid}/retry-payment", RetryPayment);
    }

    private static async Task<IResult> GetBookingById(
        Guid bookingId,
        [FromQuery] string? customerSessionId,
        IMessageBus bus,
        CancellationToken ct)
    {
            var dto = await bus.InvokeAsync<BookingDetailsDto?>(
                new GetBookingByIdQuery
                {
                    BookingId = bookingId,
                    CustomerSessionId = customerSessionId ?? string.Empty,
                    CorrelationId = string.Empty
                },
                ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
    }

    private static async Task<IResult> GetBookingsByCustomerId(
        Guid customerId,
        [FromQuery] int pageNumber,
        [FromQuery] int pageSize,
        [FromQuery] DateOnly? date,
        HttpContext http,
        IMessageBus bus,
        CancellationToken ct)
    {
        var query = new GetBookingHistoryByCustomerIdQuery
        {
            CustomerId = customerId,
            PageNumber = pageNumber <= 0 ? 1 : pageNumber,
            PageSize = pageSize <= 0 ? 20 : pageSize,
            Date = date
        };
        query.CorrelationId = http.TraceIdentifier;
        var result = await bus.InvokeAsync<PagedResult<BookingMinimalInfoDto>>(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> PreviewPricing(
        PreviewPricingQuery query,
        IMessageBus bus,
        CancellationToken ct)
    {
        var response = await bus.InvokeAsync<PreviewPricingResponse>(query, ct);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateBooking(
        CreateBookingCommand command,
        IMessageBus bus,
        CancellationToken ct)
    {
        var response = await bus.InvokeAsync<CreateBookingResponse>(command, ct);
        return Results.Created($"/api/bookings/{response.BookingId}", response);
    }

    private static async Task<IResult> CheckInBooking(
        [FromRoute(Name = "id")] Guid bookingId,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new CheckInBookingCommand { BookingId = bookingId }, ct);
        return Results.Ok();
    }

    private static async Task<IResult> CancelBooking(
        [FromRoute(Name = "id")] Guid bookingId,
        IMessageBus bus,
        CancellationToken ct)
    {
        await bus.InvokeAsync(new CancelBookingCommand { BookingId = bookingId }, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RetryPayment(
        Guid id,
        [FromBody] RetryPaymentRequest request,
        IMessageBus bus,
        CancellationToken ct)
    {
        var command = new RetryPaymentCommand
        {
            BookingId = id,
            CustomerSessionId = request.CustomerSessionId,
            PaymentMethod = request.PaymentMethod,
            ReturnUrl = request.ReturnUrl,
            IpAddress = request.IpAddress,
            ReplacePendingPayment = request.ReplacePendingPayment
        };
        var response = await bus.InvokeAsync<CreateBookingResponse>(command, ct);
        return Results.Ok(response);
    }
}

public record RetryPaymentRequest(
    string CustomerSessionId,
    string PaymentMethod,
    string ReturnUrl,
    string IpAddress,
    bool ReplacePendingPayment = false
);


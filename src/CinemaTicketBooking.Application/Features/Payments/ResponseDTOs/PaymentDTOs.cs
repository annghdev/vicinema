using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Request to create a payment via the gateway.
/// </summary>
public sealed record CreatePaymentRequest(
    Guid BookingId,
    Guid PaymentTransactionId,
    decimal Amount,
    string OrderDescription,
    string CustomerEmail,
    string ReturnUrl,
    string IpAddress);

/// <summary>
/// Result from the payment gateway after creating a payment.
/// </summary>
public sealed record CreatePaymentResult(
    bool Success,
    string PaymentUrl,
    string GatewayTransactionId,
    PaymentRedirectBehavior RedirectBehavior,
    string? ErrorMessage = null);

/// <summary>
/// Request to confirm/verify payment from gateway callback.
/// </summary>
public sealed record ConfirmPaymentRequest(
    Guid BookingId,
    string GatewayTransactionId,
    Dictionary<string, string> GatewayResponseParams);

/// <summary>
/// Result of payment confirmation.
/// </summary>
public sealed record ConfirmPaymentResult(
    bool IsSuccess,
    Guid BookingId,
    string GatewayTransactionId,
    string? ErrorMessage = null);

/// <summary>
/// DTO for available payment gateway option shown to client.
/// </summary>
public sealed record PaymentGatewayOptionDto(
    string Method,
    string DisplayName,
    PaymentRedirectBehavior RedirectBehavior,
    string Icon);

namespace CinemaTicketBooking.Application.Features;

public record CreateBookingResponse(
    Guid BookingId,
    DateTimeOffset PaymentExpiresAt,
    decimal OriginAmount,
    decimal FinalAmount,
    string PaymentStatus,
    string? PaymentUrl = null,
    PaymentRedirectBehavior? RedirectBehavior = null,
    Guid? PaymentTransactionId = null,
    string? GatewayTransactionId = null,
    decimal PromotionDiscountAmount = 0m,
    List<Features.Promotions.AppliedPromotionDto> AppliedPromotions = default!,
    List<Features.Promotions.FreeConcessionItemDto> FreeItems = default!)
{
    public List<Features.Promotions.AppliedPromotionDto> AppliedPromotions { get; init; } = AppliedPromotions ?? [];
    public List<Features.Promotions.FreeConcessionItemDto> FreeItems { get; init; } = FreeItems ?? [];
}

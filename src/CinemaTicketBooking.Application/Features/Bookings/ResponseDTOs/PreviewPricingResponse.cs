namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Price breakdown for the checkout preview, computed using the same domain logic
/// as the actual booking — including membership / loyalty discounts, coupon discounts,
/// and promotion discounts.
/// </summary>
public record PreviewPricingResponse(
    decimal OriginAmount,
    decimal TicketDiscount,
    decimal ConcessionDiscount,
    decimal TotalDiscount,
    decimal FinalAmount,
    bool IsRegisteredCustomer,
    string? LoyaltyTierName,
    string? LoyaltyTierDescription,
    decimal? TicketDiscountPercent,
    decimal? ConcessionDiscountPercent,
    // Coupon discount info
    decimal CouponDiscountAmount = 0m,
    string? CouponCode = null,
    string? CouponDescription = null,
    // Promotion discount info
    decimal PromotionDiscountAmount = 0m,
    List<Features.Promotions.AppliedPromotionDto> AppliedPromotions = default!,
    List<Features.Promotions.FreeConcessionItemDto> FreeItems = default!)
{
    public List<Features.Promotions.AppliedPromotionDto> AppliedPromotions { get; init; } = AppliedPromotions ?? [];
    public List<Features.Promotions.FreeConcessionItemDto> FreeItems { get; init; } = FreeItems ?? [];
}

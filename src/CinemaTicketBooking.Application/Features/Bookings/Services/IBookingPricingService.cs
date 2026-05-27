using CinemaTicketBooking.Application.Features.Promotions;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Determines whether the pricing calculation is for a live booking (with state mutation)
/// or a preview (read-only, no DB writes).
/// </summary>
public enum PricingMode { Preview, Booking }

/// <summary>
/// Aggregates all inputs required to calculate pricing for a booking or preview.
/// Includes the booking entity, customer, showtime, selected tickets, and optional coupon.
/// </summary>
public record PricingContext(
    Booking Booking,
    Customer? Customer,
    ShowTime ShowTime,
    List<Ticket> SelectedTickets,
    string? CouponCode,
    PricingMode Mode);

/// <summary>
/// Output of the pricing pipeline: total/coupon/promotion discounts, final amount,
/// applied promotion details, free items, and loyalty-tier display info.
/// </summary>
public record PricingResult(
    decimal TotalDiscount,
    decimal FinalAmount,
    decimal PromotionDiscountAmount,
    decimal CouponDiscountAmount,
    string? CouponCode,
    string? CouponDescription,
    List<AppliedPromotionDto> AppliedPromotions,
    List<FreeConcessionItemDto> FreeItems,
    decimal TicketDiscount,
    decimal ConcessionDiscount,
    string? LoyaltyTierName,
    string? LoyaltyTierDescription,
    decimal? TicketDiscountPercent,
    decimal? ConcessionDiscountPercent);

/// <summary>
/// Shared pricing pipeline used by both booking creation and pricing preview.
/// Orchestrates promotion scanning, coupon resolution, loyalty discounts, and free items.
/// </summary>
public interface IBookingPricingService
{
    /// <summary>
    /// Calculates the full pricing breakdown for the given context.
    /// Returns a PricingResult with all discount components and display info.
    /// </summary>
    Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken ct);
}

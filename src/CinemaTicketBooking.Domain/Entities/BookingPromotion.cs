using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Domain;

public class BookingPromotion : BaseEntity
{
    public Guid BookingId { get; set; }
    public Guid PromotionProgramId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public PromotionDiscountType DiscountType { get; set; }

    public static BookingPromotion Create(
        Guid bookingId,
        Guid promotionProgramId,
        string name,
        decimal discountAmount,
        PromotionDiscountType discountType)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Promotion name cannot be empty.", nameof(name));

        if (discountAmount < 0)
            throw new ArgumentException("Discount amount cannot be negative.", nameof(discountAmount));

        return new BookingPromotion
        {
            BookingId = bookingId,
            PromotionProgramId = promotionProgramId,
            PromotionName = name,
            DiscountAmount = discountAmount,
            DiscountType = discountType
        };
    }
}

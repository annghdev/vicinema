namespace CinemaTicketBooking.Domain;

public class CustomerPromotionUsage : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Guid PromotionProgramId { get; set; }
    public Guid BookingId { get; set; }
    public DateTimeOffset UsedAt { get; set; }

    public static CustomerPromotionUsage Create(
        Guid customerId,
        Guid promotionProgramId,
        Guid bookingId)
    {
        return new CustomerPromotionUsage
        {
            CustomerId = customerId,
            PromotionProgramId = promotionProgramId,
            BookingId = bookingId,
            UsedAt = DateTimeOffset.UtcNow
        };
    }
}

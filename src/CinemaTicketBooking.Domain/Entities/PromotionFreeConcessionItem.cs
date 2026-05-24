namespace CinemaTicketBooking.Domain;

public class PromotionFreeConcessionItem : BaseEntity
{
    public Guid PromotionProgramId { get; set; }
    public Guid ConcessionId { get; set; }
    public int Quantity { get; set; }

    public static PromotionFreeConcessionItem Create(
        Guid promotionProgramId,
        Guid concessionId,
        int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        return new PromotionFreeConcessionItem
        {
            PromotionProgramId = promotionProgramId,
            ConcessionId = concessionId,
            Quantity = quantity
        };
    }
}

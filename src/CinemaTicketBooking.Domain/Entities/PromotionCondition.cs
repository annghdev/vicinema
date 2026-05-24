using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Domain;

public class PromotionCondition : BaseEntity
{
    public Guid PromotionProgramId { get; set; }
    public ConditionType ConditionType { get; set; }
    public string? Value { get; set; }
    public string? SecondaryValue { get; set; }

    public static PromotionCondition Create(
        Guid promotionProgramId,
        ConditionType conditionType,
        string? value,
        string? secondaryValue = null)
    {
        return new PromotionCondition
        {
            PromotionProgramId = promotionProgramId,
            ConditionType = conditionType,
            Value = value,
            SecondaryValue = secondaryValue
        };
    }
}

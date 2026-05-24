namespace CinemaTicketBooking.Application.Features.Promotions;

public static class PromotionCacheKeys
{
    public const string ListPrefix = "Promotions_List_";
    public const string ActiveKey = "Promotions_Active";

    public static string Detail(Guid id) => $"Promotions_Detail_{id}";
}

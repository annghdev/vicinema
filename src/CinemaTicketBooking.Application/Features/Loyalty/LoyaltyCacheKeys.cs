namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// Cache key constants for loyalty-related queries.
/// </summary>
public static class LoyaltyCacheKeys
{
    public const string CustomerLoyaltyPrefix = "Loyalty:Customer:";
    public const string TiersListPrefix = "Loyalty:Tiers:";
    public const string ActiveTiersKey = "Loyalty:Tiers:Active";
    public const string TierByIdPrefix = "Loyalty:Tier:";

    public static string CustomerLoyalty(Guid customerId) => $"{CustomerLoyaltyPrefix}{customerId}";
    public static string ActiveTiers() => ActiveTiersKey;
    public static string TierById(Guid id) => $"{TierByIdPrefix}{id}";
}

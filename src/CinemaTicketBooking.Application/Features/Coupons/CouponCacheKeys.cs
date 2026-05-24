namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Cache key constants for coupon queries.
/// </summary>
public static class CouponCacheKeys
{
    public const string CustomerCouponsPrefix = "Coupon:Customer:";
    public const string PublicTemplatesKey = "Coupon:Templates:Public";
    public const string TemplateByCodePrefix = "Coupon:Template:Code:";
    public const string AllTemplatesKey = "Coupon:Templates:All";

    public static string CustomerCoupons(Guid customerId) => $"{CustomerCouponsPrefix}{customerId}";
    public static string TemplateByCode(string code) => $"{TemplateByCodePrefix}{code.ToUpperInvariant()}";
}
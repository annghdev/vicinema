namespace CinemaTicketBooking.Domain;

/// <summary>
/// Type of discount a coupon provides.
/// </summary>
public enum DiscountType
{
    /// <summary>Fixed amount deducted (in VND).</summary>
    Fixed = 0,

    /// <summary>Percentage of the qualifying amount deducted.</summary>
    Percentage = 1
}
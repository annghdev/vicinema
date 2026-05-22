namespace CinemaTicketBooking.Domain;

/// <summary>
/// Defines the distribution type of a coupon.
/// </summary>
public enum CouponType
{
    /// <summary>Public / shared code (KOL, promotion) — limited usage, multiple customers can use.</summary>
    Public = 0,

    /// <summary>Personal, one-time coupon assigned to a specific customer.</summary>
    Personal = 1
}
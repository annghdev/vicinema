namespace CinemaTicketBooking.Domain;

/// <summary>
/// Loyalty tier levels for the membership program.
/// Points are accumulated from successful bookings (1000 VND = 1 point).
/// </summary>
public enum LoyaltyTier
{
    /// <summary>Đồng — entry tier (0–100 points)</summary>
    Bronze = 0,

    /// <summary>Bạc — (101–500 points)</summary>
    Silver = 1,

    /// <summary>Vàng — (501–1,500 points)</summary>
    Gold = 2,

    /// <summary>Bạch Kim — (1,501–5,000 points)</summary>
    Platinum = 3,

    /// <summary>Kim Cương — (5,001–15,000 points)</summary>
    Diamond = 4,

    /// <summary>Ruby — (over 15,000 points)</summary>
    Ruby = 5
}

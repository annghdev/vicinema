namespace CinemaTicketBooking.Domain;

/// <summary>
/// Defines what items a coupon discount applies to.
/// </summary>
public enum DiscountScope
{
    /// <summary>Applies to the entire bill (tickets + concessions).</summary>
    All = 0,

    /// <summary>Only applies to ticket prices.</summary>
    Tickets = 1,

    /// <summary>Only applies to concession (snack/drink) prices.</summary>
    Concessions = 2
}
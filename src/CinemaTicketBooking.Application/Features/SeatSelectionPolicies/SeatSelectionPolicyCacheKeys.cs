namespace CinemaTicketBooking.Application.Features;

public static class SeatSelectionPolicyCacheKeys
{
    public const string ListPrefix = "SeatSelectionPolicies_List_";
    
    public static string GetSeatSelectionPolicyById(Guid id) => $"SeatSelectionPolicy_ById_{id}";
        
    public static string GetSeatSelectionPolicies(bool? isActive) => $"{ListPrefix}All_{isActive}";
}

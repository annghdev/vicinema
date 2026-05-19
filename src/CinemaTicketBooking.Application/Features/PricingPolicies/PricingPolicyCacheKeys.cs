namespace CinemaTicketBooking.Application.Features;

public static class PricingPolicyCacheKeys
{
    public const string ListPrefix = "PricingPolicies_List_";
    
    public static string GetPricingPolicyById(Guid id) => $"PricingPolicy_ById_{id}";
    
    public static string GetPagedPricingPolicies(int page, int size, Guid? cinemaId, Domain.ScreenType? screenType, Domain.SeatType? seatType, bool? isActive, string sortBy, string sortDir) 
        => $"{ListPrefix}Paged_{page}_{size}_{cinemaId}_{screenType}_{seatType}_{isActive}_{sortBy}_{sortDir}";
        
    public static string GetPricingPolicies(Guid? cinemaId) => $"{ListPrefix}All_{cinemaId}";
    
    public static string GetPricingPolicyDropdown(Guid? cinemaId, bool onlyActive) 
        => $"{ListPrefix}Dropdown_{cinemaId}_{onlyActive}";
}

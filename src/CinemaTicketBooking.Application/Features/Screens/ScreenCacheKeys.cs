namespace CinemaTicketBooking.Application.Features;

public static class ScreenCacheKeys
{
    public const string ListPrefix = "Screens_List_";
    
    public static string GetScreenById(Guid id) => $"Screen_ById_{id}";
    
    public static string GetPagedScreens(int page, int size, Guid? cinemaId, Domain.ScreenType? screenType, bool? isActive, string sortBy, string sortDir) 
        => $"{ListPrefix}Paged_{page}_{size}_{cinemaId}_{screenType}_{isActive}_{sortBy}_{sortDir}";
        
    public static string GetScreens(Guid? cinemaId) => $"{ListPrefix}All_{cinemaId}";
    
    public static string GetScreenDropdown(Guid? cinemaId, bool onlyActive, int maxItems) 
        => $"{ListPrefix}Dropdown_{cinemaId}_{onlyActive}_{maxItems}";
}

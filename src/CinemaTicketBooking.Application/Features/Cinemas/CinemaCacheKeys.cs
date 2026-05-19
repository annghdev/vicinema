namespace CinemaTicketBooking.Application.Features;

public static class CinemaCacheKeys
{
    public const string ListPrefix = "Cinemas_List_";
    
    public static string GetCinemaById(Guid id) => $"Cinema_ById_{id}";
    
    public static string GetPagedCinemas(int page, int size, string? search, bool? isActive, string sortBy, string sortDir) 
        => $"{ListPrefix}Paged_{page}_{size}_{search}_{isActive}_{sortBy}_{sortDir}";
        
    public static string GetCinemas(bool? isActive) => $"{ListPrefix}All_{isActive}";
    
    public static string GetCinemaDropdown(string? search, bool onlyActive, int maxItems) 
        => $"{ListPrefix}Dropdown_{search}_{onlyActive}_{maxItems}";
}

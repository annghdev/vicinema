namespace CinemaTicketBooking.Application.Features;

public static class ConcessionCacheKeys
{
    public const string ListPrefix = "Concessions_List_";
    
    public static string GetConcessionById(Guid id) => $"Concession_ById_{id}";
    
    public static string GetPagedConcessions(int page, int size, string? search, bool? isAvailable, string sortBy, string sortDir) 
        => $"{ListPrefix}Paged_{page}_{size}_{search}_{isAvailable}_{sortBy}_{sortDir}";
        
    public static string GetConcessions() => $"{ListPrefix}All";
    
    public static string GetConcessionDropdown(string? search, bool? isAvailable, int maxItems) 
        => $"{ListPrefix}Dropdown_{search}_{isAvailable}_{maxItems}";
}

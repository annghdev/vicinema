namespace CinemaTicketBooking.Application.Features;

public static class ShowTimeCacheKeys
{
    public const string ListPrefix = "ShowTimes_List_";
    
    public static string GetShowTimes(Guid? cinemaId, Guid? movieId, Guid? screenId, Domain.ShowTimeStatus? status, DateOnly? date) 
        => $"{ListPrefix}All_{cinemaId}_{movieId}_{screenId}_{status}_{date}";
    
    public static string GetShowTimeDropdown(Guid? cinemaId, Guid? movieId, Guid? screenId, Domain.ShowTimeStatus? status, int maxItems) 
        => $"{ListPrefix}Dropdown_{cinemaId}_{movieId}_{screenId}_{status}_{maxItems}";
}

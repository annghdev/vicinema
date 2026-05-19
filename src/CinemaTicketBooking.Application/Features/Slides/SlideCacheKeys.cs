namespace CinemaTicketBooking.Application.Features;

public static class SlideCacheKeys
{
    public const string ListPrefix = "Slides_List_";
    
    public static string GetActiveSlides() => $"{ListPrefix}Active";
}

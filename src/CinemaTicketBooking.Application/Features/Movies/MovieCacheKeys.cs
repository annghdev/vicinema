namespace CinemaTicketBooking.Application.Features;

public static class MovieCacheKeys
{
    public const string ListPrefix = "Movies_List_";
    
    public static string GetMovieById(Guid id) => $"Movie_ById_{id}";
    
    public static string GetPagedMovies(int page, int size, string? search, Domain.MovieStatus? status, Domain.MovieGenre? genre, string sortBy, string sortDir) 
        => $"{ListPrefix}Paged_{page}_{size}_{search}_{status}_{genre}_{sortBy}_{sortDir}";
        
    public static string GetMovies() => $"{ListPrefix}All";
    
    public static string GetMovieDropdown(string? search, Domain.MovieStatus? status, int maxItems) 
        => $"{ListPrefix}Dropdown_{search}_{status}_{maxItems}";
        
    public static string GetUpcomingAndNowShowing() => $"{ListPrefix}UpcomingNowShowing";
    
    public static string GetUpcomingAndNowShowingDropdown(string? search, int maxItems) 
        => $"{ListPrefix}UpcomingNowShowingDropdown_{search}_{maxItems}";
}

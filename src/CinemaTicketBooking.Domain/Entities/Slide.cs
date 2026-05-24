namespace CinemaTicketBooking.Domain;

/// <summary>
/// Represents a promotional slide for the frontend homepage carousel.
/// </summary>
public class Slide : AggregateRoot
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public SlideType Type { get; set; } = SlideType.ShowingMovie;
    public string? VideoUrl { get; set; }

    // =============================================================
    // Factory and data mutation
    // =============================================================

    public static Slide Create(
        string title,
        string description,
        string imageUrl,
        string targetUrl,
        int displayOrder,
        SlideType type = SlideType.ShowingMovie,
        string? videoUrl = null,
        bool isActive = true)
    {
        var slide = new Slide
        {
            Title = title,
            Description = description,
            ImageUrl = imageUrl,
            TargetUrl = targetUrl,
            DisplayOrder = displayOrder,
            Type = type,
            VideoUrl = videoUrl,
            IsActive = isActive
        };

        slide.RaiseEvent(new SlideCreated());
        return slide;
    }

    public void UpdateBasicInfo(
        string title,
        string description,
        string imageUrl,
        string targetUrl,
        int displayOrder,
        SlideType type,
        string? videoUrl,
        bool isActive)
    {
        Title = title;
        Description = description;
        ImageUrl = imageUrl;
        TargetUrl = targetUrl;
        DisplayOrder = displayOrder;
        Type = type;
        VideoUrl = videoUrl;
        IsActive = isActive;

        RaiseEvent(new SlideUpdated());
    }
}

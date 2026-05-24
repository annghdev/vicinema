using CinemaTicketBooking.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all active promotional slides.
/// </summary>
public class GetActiveSlidesQuery : ICachableQuery<IReadOnlyList<SlideDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => SlideCacheKeys.GetActiveSlides();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for active slides.
/// </summary>
public class GetActiveSlidesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns active slides ordered by display order.
    /// </summary>
    public async Task<IReadOnlyList<SlideDto>> Handle(GetActiveSlidesQuery query, CancellationToken ct)
    {
        var slides = await uow.Slides
            .GetQueryFilter()
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Take(10)
            .Select(s => new SlideDto(
                s.Id,
                s.Title,
                s.Description,
                s.ImageUrl,
                s.TargetUrl,
                s.DisplayOrder,
                s.Type,
                s.VideoUrl
            ))
            .ToListAsync(ct);

        return slides;
    }
}


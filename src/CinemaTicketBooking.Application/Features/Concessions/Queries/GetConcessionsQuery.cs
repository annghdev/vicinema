using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all concession items.
/// </summary>
public class GetConcessionsQuery : ICachableQuery<IReadOnlyList<ConcessionDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ConcessionCacheKeys.GetConcessions();
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for all concession items.
/// </summary>
public class GetConcessionsHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all concessions mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<ConcessionDto>> Handle(GetConcessionsQuery query, CancellationToken ct)
    {
        var items = await uow.Concessions
            .GetQueryFilter()
            .OrderBy(x => x.Name)
            .Select(x => new ConcessionDto(
                x.Id,
                x.Name,
                x.Price,
                x.ImageUrl,
                x.IsAvailable,
                x.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}

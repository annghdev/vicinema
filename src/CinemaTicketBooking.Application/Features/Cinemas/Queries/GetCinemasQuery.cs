using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all cinemas.
/// </summary>
public class GetCinemasQuery : ICachableQuery<IReadOnlyList<CinemaDto>>
{
    public bool? IsActive { get; set; }
    public string CorrelationId { get; set; } = string.Empty;

    public string CacheKey => CinemaCacheKeys.GetCinemas(IsActive);

    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for all cinemas.
/// </summary>
public class GetCinemasHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns all cinemas mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<CinemaDto>> Handle(GetCinemasQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Cinemas.GetQueryFilter();

        var cinemas = await dbQuery
                    .Select(cinema => new CinemaDto(
                        cinema.Id,
                        cinema.Name,
                        cinema.ThumbnailUrl,
                        cinema.Geo,
                        cinema.Address,
                        cinema.IsActive,
                        cinema.CreatedAt
                    ))
                    .ToListAsync(ct);

        return cinemas;
    }
}

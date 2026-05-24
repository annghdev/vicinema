using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets showtimes with optional filters.
/// </summary>
public class GetShowTimesQuery : ICachableQuery<IReadOnlyList<ShowTimeDto>>
{
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }
    public Guid? ScreenId { get; set; }
    public ShowTimeStatus? Status { get; set; }
    public DateOnly? Date { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ShowTimeCacheKeys.GetShowTimes(CinemaId, MovieId, ScreenId, Status, Date);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for showtime list.
/// </summary>
public class GetShowTimesHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns showtimes mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<ShowTimeDto>> Handle(GetShowTimesQuery query, CancellationToken ct)
    {
        var dbQuery = uow.ShowTimes.GetQueryFilter();
        dbQuery = ApplyFilter(dbQuery, query);

        var items = await dbQuery
            .OrderBy(x => x.StartAt)
            .Select(x => new ShowTimeDto(
                x.Id,
                x.MovieId,
                x.Movie != null ? x.Movie.Name : string.Empty,
                x.Movie != null ? x.Movie.ThumbnailUrl : string.Empty,
                x.Movie != null ? x.Movie.Genre.ToString() : string.Empty,
                x.Movie != null ? x.Movie.Duration : 0,
                x.ScreenId,
                x.Screen != null ? x.Screen.Code : string.Empty,
                x.Screen != null ? x.Screen.CinemaId : Guid.Empty,
                x.Screen != null && x.Screen.Cinema != null ? x.Screen.Cinema.Name : string.Empty,
                x.Screen != null && x.Screen.Cinema != null ? x.Screen.Cinema.Address : string.Empty,
                x.Date,
                x.StartAt,
                x.EndAt,
                x.Status,
                x.Format,
                x.Tickets.Count,
                // Only Available is bookable; Locking/PendingPayment/Sold are unavailable.
                x.Tickets.Count(t => t.Status == TicketStatus.Available),
                x.CreatedAt))
            .ToListAsync(ct);

        return items;
    }

    private static IQueryable<ShowTime> ApplyFilter(IQueryable<ShowTime> dbQuery, GetShowTimesQuery query)
    {
        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Screen != null && x.Screen.CinemaId == query.CinemaId.Value);
        }

        if (query.MovieId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.MovieId == query.MovieId.Value);
        }

        if (query.ScreenId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ScreenId == query.ScreenId.Value);
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Status == query.Status.Value);
        }

        if (query.Date.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Date == query.Date.Value);
        }

        return dbQuery;
    }
}

/// <summary>
/// Validates showtime list query payload.
/// </summary>
public class GetShowTimesValidator : AbstractValidator<GetShowTimesQuery>
{
    public GetShowTimesValidator()
    {
        RuleFor(x => x.CinemaId)
            .Must(x => !x.HasValue || x.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");

        RuleFor(x => x.MovieId)
            .Must(x => !x.HasValue || x.Value != Guid.Empty)
            .WithMessage("Movie ID is invalid.");

        RuleFor(x => x.ScreenId)
            .Must(x => !x.HasValue || x.Value != Guid.Empty)
            .WithMessage("Screen ID is invalid.");

        RuleFor(x => x.Status)
            .Must(status => status is null || Enum.IsDefined(status.Value))
            .WithMessage("Invalid showtime status.");
    }
}

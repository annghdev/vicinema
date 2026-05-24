using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets screens, optionally filtered by cinema.
/// </summary>
public class GetScreensQuery : ICachableQuery<IReadOnlyList<ScreenDto>>
{
    public Guid? CinemaId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ScreenCacheKeys.GetScreens(CinemaId);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for screens list.
/// </summary>
public class GetScreensHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns screens mapped to read models.
    /// </summary>
    public async Task<IReadOnlyList<ScreenDto>> Handle(GetScreensQuery query, CancellationToken ct)
    {
        var dbQuery = uow.Screens.GetQueryFilter();

        if (query.CinemaId.HasValue)
        {
            dbQuery = dbQuery.Where(screen => screen.CinemaId == query.CinemaId.Value);
        }

        var items = await dbQuery
            .OrderBy(screen => screen.Code)
            .Select(screen => new ScreenDto(
                screen.Id,
                screen.CinemaId,
                screen.Code,
                screen.RowOfSeats,
                screen.ColumnOfSeats,
                screen.TotalSeats,
                screen.SeatMap,
                screen.SupportedFormats,
                screen.IsActive,
                screen.Seats.Count(seat => seat.IsActive),
                screen.CreatedAt
            ))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates screen list query payload.
/// </summary>
public class GetScreensValidator : AbstractValidator<GetScreensQuery>
{
    public GetScreensValidator()
    {
        RuleFor(x => x.CinemaId)
            .Must(cinemaId => !cinemaId.HasValue || cinemaId.Value != Guid.Empty)
            .WithMessage("Cinema ID is invalid.");
    }
}

using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets showtimes for dropdown data source.
/// </summary>
public class GetShowTimeDropdownQuery : ICachableQuery<IReadOnlyList<ShowTimeDropdownDto>>
{
    public Guid? CinemaId { get; set; }
    public Guid? MovieId { get; set; }
    public Guid? ScreenId { get; set; }
    public ShowTimeStatus? Status { get; set; }
    public int MaxItems { get; set; } = 100;
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ShowTimeCacheKeys.GetShowTimeDropdown(CinemaId, MovieId, ScreenId, Status, MaxItems);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for lightweight showtime dropdown list.
/// </summary>
public class GetShowTimeDropdownHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns lightweight dropdown items with optimized query shape.
    /// </summary>
    public async Task<IReadOnlyList<ShowTimeDropdownDto>> Handle(GetShowTimeDropdownQuery query, CancellationToken ct)
    {
        var dbQuery = uow.ShowTimes.GetQueryFilter();

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

        var items = await dbQuery
            .OrderBy(x => x.StartAt)
            .Take(query.MaxItems)
            .Select(x => new ShowTimeDropdownDto(
                x.Id,
                $"{x.StartAt:yyyy-MM-dd HH:mm} | {(x.Movie != null ? x.Movie.Name : string.Empty)} | {(x.Screen != null ? x.Screen.Code : string.Empty)}"))
            .ToListAsync(ct);

        return items;
    }
}

/// <summary>
/// Validates dropdown query payload.
/// </summary>
public class GetShowTimeDropdownValidator : AbstractValidator<GetShowTimeDropdownQuery>
{
    public GetShowTimeDropdownValidator()
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

        RuleFor(x => x.MaxItems)
            .InclusiveBetween(1, 500)
            .WithMessage("MaxItems must be between 1 and 500.");
    }
}

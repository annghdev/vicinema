using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets a screen by id.
/// </summary>
public class GetScreenByIdQuery : ICachableQuery<ScreenDetailDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ScreenCacheKeys.GetScreenById(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for a specific screen.
/// </summary>
public class GetScreenByIdHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns screen data when found; otherwise null.
    /// </summary>
    public async Task<ScreenDetailDto?> Handle(GetScreenByIdQuery query, CancellationToken ct)
    {
        var screen = await uow.Screens
            .GetQueryFilter()
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new ScreenDetailDto(
                x.Id,
                x.CinemaId,
                x.Code,
                x.RowOfSeats,
                x.ColumnOfSeats,
                x.TotalSeats,
                x.SeatMap,
                x.Type,
                x.IsActive,
                x.CreatedAt,
                x.Seats
                    .OrderBy(s => s.Row)
                    .ThenBy(s => s.Column)
                    .Select(s => new ScreenSeatDto(
                        s.Id,
                        s.Code,
                        s.Row,
                        s.Column,
                        s.Type,
                        s.IsAvailable,
                        s.IsActive))
                    .ToList()))
            .FirstOrDefaultAsync(ct);

        return screen;
    }
}

/// <summary>
/// Validates query payload.
/// </summary>
public class GetScreenByIdValidator : AbstractValidator<GetScreenByIdQuery>
{
    public GetScreenByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Screen ID is required.");
    }
}

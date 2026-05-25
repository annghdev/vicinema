namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Read model used by get-screen-by-id queries.
/// </summary>
public sealed record ScreenDetailDto(
    Guid Id,
    Guid CinemaId,
    string Code,
    int RowOfSeats,
    int ColumnOfSeats,
    int TotalSeats,
    string SeatMap,
    ScreenType Type,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ScreenSeatDto> Seats
);

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Read model used by screen queries.
/// </summary>
public sealed record ScreenDto(
    Guid Id,
    Guid CinemaId,
    string Code,
    int RowOfSeats,
    int ColumnOfSeats,
    int TotalSeats,
    string SeatMap,
    List<ScreenType> SupportedFormats,
    bool IsActive,
    int ActiveSeatCount,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Lightweight read model for seat details in a screen.
/// </summary>
public sealed record ScreenSeatDto(
    Guid Id,
    string Code,
    int Row,
    int Column,
    SeatType Type,
    bool IsAvailable,
    bool IsActive
);

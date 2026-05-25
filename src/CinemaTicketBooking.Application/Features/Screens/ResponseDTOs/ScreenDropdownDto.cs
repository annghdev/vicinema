namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Lightweight DTO for screen dropdowns.
/// </summary>
public sealed record ScreenDropdownDto(
    Guid Id,
    string Code,
    Guid CinemaId
);

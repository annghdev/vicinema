using CinemaTicketBooking.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Handles the <see cref="BookingConfirmed"/> domain event by sending a
/// professional booking-confirmation email that contains showtime details,
/// seat list, and a QR check-in code.
/// </summary>
public class SendEmailBookingConfirmedHandler(
    IEmailSender emailSender,
    IBookingRepository bookingRepository,
    IShowTimeRepository showTimeRepository,
    ILogger<SendEmailBookingConfirmedHandler> logger)
{
    /// <summary>
    /// Wolverine message handler — invoked after a booking is confirmed.
    /// </summary>
    public async Task Handle(BookingConfirmed @event, CancellationToken cancellationToken)
    {
        // 1. Guard: skip if no recipient address
        if (string.IsNullOrWhiteSpace(@event.Email))
        {
            logger.LogWarning(
                "BookingConfirmed {BookingId}: no email address — notification skipped.",
                @event.BookingId);
            return;
        }

        // 2. Load full booking to retrieve individual ticket details
        var booking = await bookingRepository.LoadFullAsync(@event.BookingId, cancellationToken);
        if (booking is null)
        {
            logger.LogWarning(
                "BookingConfirmed {BookingId}: booking not found — email skipped.",
                @event.BookingId);
            return;
        }

        // 3. Load full showtime to retrieve movie and screen/cinema info
        var showTime = await showTimeRepository.LoadFullAsync(@event.ShowTimeId, cancellationToken);

        // 4. Build line items from the loaded booking graph
        var ticketLines = booking.Tickets
            .Select(bt => new TicketLineItem
            {
                SeatCode = bt.Ticket?.SeatCode ?? bt.TicketId.ToString()[..8],
                SeatType = bt.Ticket?.Description ?? "Standard",
                Price    = bt.Ticket?.Price ?? 0m
            })
            .ToList();

        var concessionLines = booking.Concessions
            .Select(bc => new ConcessionLineItem
            {
                Name     = bc.Concession?.Name ?? "Dịch vụ",
                Quantity = bc.Quantity,
                Price    = bc.Concession?.Price ?? 0m
            })
            .ToList();

        // 5. Resolve display values from navigations (with safe fallbacks)
        var movieName   = showTime?.Movie?.Name   ?? "Phim chiếu";
        var cinemaName  = showTime?.Screen?.Cinema?.Name ?? "Cinema";
        var screenCode  = showTime?.Screen?.Code  ?? showTime?.ScreenId.ToString()[..8] ?? "—";
        var screenFormat= showTime?.Format.ToString() ?? "Standard";
        var endAt       = showTime?.EndAt ?? @event.ShowTimeStartAt.AddHours(2);

        // 6. Compose the email model
        var bookingCode = @event.BookingId.ToString("N")[..8].ToUpper();

        var model = new BookingConfirmationEmailModel
        {
            RecipientEmail  = @event.Email,
            RecipientName   = @event.CustomerName,
            BookingCode     = bookingCode,
            BookingId       = @event.BookingId.ToString(),
            MovieName       = movieName,
            CinemaName      = cinemaName,
            ScreenCode      = screenCode,
            ScreenFormat    = screenFormat,
            ShowTimeStartAt = @event.ShowTimeStartAt,
            ShowTimeEndAt   = endAt,
            Tickets         = ticketLines,
            Concessions     = concessionLines,
            TotalAmount     = @event.FinalAmount
        };

        // 7. Send confirmation email (non-blocking failure)
        try
        {
            var htmlBody = BookingConfirmationTemplate.Render(model);
            var subject = $"[Vicinema] Xác nhận đặt vé – {movieName} – #{bookingCode}";
            await emailSender.SendEmailAsync(@event.Email, subject, htmlBody, @event.CustomerName, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow — email failure must not roll back the booking
            logger.LogError(ex,
                "Failed to send booking confirmation email for {BookingId} to {Email}.",
                @event.BookingId,
                @event.Email);
        }
    }
}

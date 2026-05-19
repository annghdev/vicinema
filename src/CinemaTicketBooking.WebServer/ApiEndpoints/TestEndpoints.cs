using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace CinemaTicketBooking.WebServer.ApiEndpoints;

public static class TestEndpoints
{
    public static void MapTestEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/test")
            .RequireRateLimiting("fixed")
            .WithTags("Test");

        group.MapPost("/brevo", async ([FromServices] IEmailSender emailSender, [FromQuery] string email = "nghuuan2803@gmail.com") =>
        {
            var model = new BookingConfirmationEmailModel
            {
                RecipientEmail = email,
                RecipientName = "Nguyễn Hữu An",
                BookingCode = "B1234567",
                BookingId = Guid.CreateVersion7().ToString(),
                MovieName = "Avengers: Endgame",
                CinemaName = "CGV Vincom Center",
                ScreenCode = "Screen 1",
                ScreenFormat = "IMAX 3D",
                ShowTimeStartAt = DateTimeOffset.UtcNow.AddDays(1),
                ShowTimeEndAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(3),
                Tickets =
                [
                    new TicketLineItem { SeatCode = "A1", SeatType = "VIP", Price = 150000 },
                    new TicketLineItem { SeatCode = "A2", SeatType = "VIP", Price = 150000 }
                ],
                Concessions =
                [
                    new ConcessionLineItem { Name = "Bắp rang bơ (L)", Quantity = 1, Price = 65000 },
                    new ConcessionLineItem { Name = "Coca Cola (L)", Quantity = 2, Price = 35000 }
                ],
                TotalAmount = 435000
            };

            var htmlBody = BookingConfirmationTemplate.Render(model);
            var subject = $"[Test] Xác nhận đặt vé – {model.MovieName} – #{model.BookingCode}";

            await emailSender.SendEmailAsync(model.RecipientEmail, subject, htmlBody, model.RecipientName);

            return Results.Ok(new { message = $"Test email sent to {model.RecipientEmail}" });
        })
        .WithDescription("Sends a test booking confirmation email via Brevo.");
    }
}


using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using Microsoft.Extensions.Logging;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Sends congratulatory email when a customer's loyalty tier is upgraded.
/// </summary>
public class LoyaltyTierUpgradedEmailHandler(
    IEmailSender emailSender,
    IUnitOfWork uow,
    ILogger<LoyaltyTierUpgradedEmailHandler> logger)
{
    /// <summary>
    /// Wolverine message handler — invoked after <see cref="LoyaltyTierUpgraded"/>.
    /// </summary>
    public async Task Handle(LoyaltyTierUpgraded @event, CancellationToken ct)
    {
        // 1. Load customer for email
        var customer = await uow.Customers.GetByIdAsync(@event.CustomerId, ct);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
        {
            logger.LogWarning(
                "LoyaltyTierUpgraded {CustomerId}: no email — notification skipped.",
                @event.CustomerId);
            return;
        }

        // 2. Build email content
        var subject = $"[Vicinema] Chúc mừng bạn đã thăng hạng lên {GetTierDisplayName(@event.NewTier)}!";

        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #1a73e8;">Chúc mừng {customer.Name}!</h2>
                <p>Bạn vừa được thăng hạng từ <strong>{GetTierDisplayName(@event.PreviousTier)}</strong> lên <strong style="color: #e67e22;">{GetTierDisplayName(@event.NewTier)}</strong>!</p>
                <p>Tổng điểm tích lũy hiện tại: <strong>{@event.TotalPoints:N0} PTS</strong></p>
                <p>Đăng nhập vào tài khoản để xem các phúc lợi mới của bạn.</p>
                <br/>
                <p style="color: #888; font-size: 12px;">Vicinema — Đặt vé xem phim dễ dàng</p>
            </div>
            """;

        // 3. Send email (non-blocking failure)
        try
        {
            await emailSender.SendEmailAsync(customer.Email, subject, body, customer.Name, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to send tier upgrade email to {Email} for customer {CustomerId}",
                customer.Email, customer.Id);
        }
    }

    private static string GetTierDisplayName(LoyaltyTier tier) => tier switch
    {
        LoyaltyTier.Bronze => "Đồng",
        LoyaltyTier.Silver => "Bạc",
        LoyaltyTier.Gold => "Vàng",
        LoyaltyTier.Platinum => "Bạch Kim",
        LoyaltyTier.Diamond => "Kim Cương",
        LoyaltyTier.Ruby => "Ruby",
        _ => tier.ToString()
    };
}

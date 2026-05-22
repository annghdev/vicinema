using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Application.Features.Coupons;
using CinemaTicketBooking.Domain;
using Microsoft.Extensions.Logging;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Sends congratulatory email when a customer's loyalty tier is upgraded.
/// Also issues a personal coupon if the new tier configuration has a <see cref="LoyaltyTierConfiguration.CouponTemplateId"/>.
/// </summary>
public class LoyaltyTierUpgradedEmailHandler(
    IEmailSender emailSender,
    IUnitOfWork uow,
    ICacheService cacheService,
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

        // 2. Check if the new tier has a coupon template for upgrade rewards
        string? couponSection = null;
        var tierConfig = await uow.LoyaltyTiers.GetByTierAsync(@event.NewTier, ct);
        if (tierConfig?.CouponTemplateId is not null)
        {
            var template = await uow.CouponTemplates.GetByIdAsync(tierConfig.CouponTemplateId.Value, ct);
            if (template?.IsActive == true)
            {
                // Issue a personal coupon
                var customerCoupon = CustomerCoupon.IssueFromTemplate(
                    @event.CustomerId, template, DateTimeOffset.UtcNow);
                uow.CustomerCoupons.Add(customerCoupon);
                await uow.CommitAsync(ct);

                // Format coupon description for email
                var discountText = template.DiscountType == DiscountType.Fixed
                    ? $"{template.DiscountValue:N0}đ"
                    : $"{template.DiscountValue}%{(template.MaxDiscountAmount.HasValue ? $" (tối đa {template.MaxDiscountAmount:N0}đ)" : "")}";

                    var expiryFormatted = DateTimeOffset.UtcNow.AddDays(template.DurationDays).ToString("dd/MM/yyyy");

                    couponSection = string.Concat(
                        @"<div style=""background: #f0fdf4; border: 1px solid #86efac; border-radius: 12px; padding: 20px; margin: 20px 0;"">",
                        @"<h3 style=""color: #16a34a; margin: 0 0 8px 0;"">🎉 Quà tặng thăng hạng</h3>",
                        @"<p style=""margin: 0 0 4px 0;"">Bạn đã nhận được mã giảm giá <strong>", template.Code, "</strong></p>",
                        @"<p style=""margin: 0; font-size: 14px; color: #166534;"">Giảm <strong>", discountText, "</strong> — ", template.Description, "</p>",
                        @"<p style=""margin: 8px 0 0 0; font-size: 12px; color: #666;"">Hạn sử dụng: ", expiryFormatted, "</p>",
                        "</div>");

                // Invalidate coupon cache
                await cacheService.RemoveAsync(
                    CouponCacheKeys.CustomerCoupons(@event.CustomerId), ct);

                logger.LogInformation(
                    "Issued personal coupon {CouponCode} to customer {CustomerId} for tier upgrade to {NewTier}",
                    template.Code, @event.CustomerId, @event.NewTier);
            }
        }

        // 2. Build email content
        var subject = $"[Vicinema] Chúc mừng bạn đã thăng hạng lên {GetTierDisplayName(@event.NewTier)}!";

        var body = $"""
            <div style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;">
                <h2 style="color: #1a73e8;">Chúc mừng {customer.Name}!</h2>
                <p>Bạn vừa được thăng hạng từ <strong>{GetTierDisplayName(@event.PreviousTier)}</strong> lên <strong style="color: #e67e22;">{GetTierDisplayName(@event.NewTier)}</strong>!</p>
                <p>Tổng điểm tích lũy hiện tại: <strong>{@event.TotalPoints:N0} PTS</strong></p>
                <p>Đăng nhập vào tài khoản để xem các phúc lợi mới của bạn.</p>
                {couponSection ?? ""}
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

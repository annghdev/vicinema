using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Messaging;

/// <summary>
/// Handles the CouponApplied domain event after a coupon is applied to a booking.
/// Marks the coupon as used, increments the template usage count,
/// and creates a usage record for public coupons.
/// Runs after the booking transaction commits via Wolverine durable inbox.
/// </summary>
public class CouponUsageTrackingHandler(IUnitOfWork uow)
{
    /// <summary>
    /// 1. Try to resolve the customer's personal coupon by CustomerId + CouponCode.
    /// 2. If found, mark it as used and update.
    /// 3. If not found, resolve the public coupon template, increment usage, and create a usage record.
    /// </summary>
    public async Task Handle(CouponApplied @event, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        CustomerCoupon? customerCoupon = null;

        // 1. Resolve personal coupon if customer is identified
        if (@event.CustomerId.HasValue)
        {
            customerCoupon = await uow.CustomerCoupons.GetByCustomerAndCodeAsync(
                @event.CustomerId.Value, @event.CouponCode, ct);
        }

        // 2. Mark personal coupon as used
        if (customerCoupon is not null)
        {
            customerCoupon.MarkUsed(now);
            uow.CustomerCoupons.Update(customerCoupon);
        }
        else
        {
            // 3. Handle public coupon: increment template usage and create usage record
            var template = await uow.CouponTemplates.GetByCodeAsync(@event.CouponCode, ct);
            if (template is not null)
            {
                template.IncrementUsage();
                uow.CouponTemplates.Update(template);

                var usageRecord = CustomerCoupon.RedeemPublic(
                    @event.CustomerId ?? Guid.Empty, template, now);
                usageRecord.MarkUsed(now);
                uow.CustomerCoupons.Add(usageRecord);
            }
        }

        await uow.CommitAsync(ct);
    }
}

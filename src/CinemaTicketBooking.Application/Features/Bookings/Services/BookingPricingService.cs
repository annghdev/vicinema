using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.Application.Features.Promotions;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Shared pricing pipeline used by both booking creation and pricing preview.
/// Orchestrates promotion scanning → coupon resolution → loyalty/strategy discount composite,
/// and computes the final pricing breakdown. Does NOT persist side-effects (coupon usage,
/// promotion usage) — those are handled by Wolverine event handlers after commit.
/// </summary>
public class BookingPricingService(
    IUnitOfWork uow,
    IPromotionScanService promotionScanService,
    ILoyaltyDiscountService loyaltyDiscountService,
    IEnumerable<IDiscountStrategy> discountStrategies) : IBookingPricingService
{
    /// <summary>
    /// Calculates the full pricing breakdown:
    /// 1. Compute base ticket + concession amounts.
    /// 2. Scan active promotions, apply them to the booking, and resolve free items.
    /// 3. Resolve coupon discount (personal or public) and apply it.
    /// 4. Compute loyalty-tier + strategy total discount.
    /// 5. Return the PricingResult with all components.
    /// </summary>
    public async Task<PricingResult> CalculateAsync(PricingContext context, CancellationToken ct)
    {
        var customer = context.Customer;
        var booking = context.Booking;
        var showTime = context.ShowTime;
        var selectedTickets = context.SelectedTickets;
        var couponCode = context.CouponCode;
        var isBooking = context.Mode == PricingMode.Booking;

        // 1. Compute base amounts from booking data
        decimal ticketAmount = booking.Tickets.Sum(bt => bt.Ticket?.Price ?? 0m);
        decimal concessionAmount = booking.Concessions.Sum(bc => (bc.Concession?.Price ?? 0m) * bc.Quantity);

        var appliedPromotions = new List<AppliedPromotionDto>();
        var freeItems = new List<FreeConcessionItemDto>();

        // =============================================
        // 2. Promotion scan and apply
        // =============================================

        var activePromotions = await uow.PromotionPrograms.GetActiveWithConditionsAsync(ct);
        if (activePromotions.Count > 0)
        {
            // 2a. Build the promotion scan context
            var scanContext = BuildPromotionScanContext(
                customer, showTime, selectedTickets, booking.OriginAmount, ticketAmount, concessionAmount);

            // 2b. Scan for applicable promotions
            var scanResult = await promotionScanService.ScanAsync(
                activePromotions.ToList(), scanContext, ct);

            // 2c. Apply each matched promotion, skipping if usage limit exceeded
            foreach (var promo in scanResult.AppliedPromotions)
            {
                var matchedPromo = activePromotions
                    .FirstOrDefault(p => p.Id == promo.PromotionProgramId);

                // Guard: check per-customer usage limit (kept inline to prevent race conditions)
                if (isBooking && customer?.IsRegistered == true && matchedPromo?.MaxUsagePerCustomer.HasValue == true)
                {
                    var existingCount = await uow.CustomerPromotionUsages
                        .GetQueryFilter()
                        .CountAsync(u => u.CustomerId == customer.Id && u.PromotionProgramId == promo.PromotionProgramId, ct);

                    if (existingCount >= matchedPromo.MaxUsagePerCustomer.Value)
                        continue;
                }

                var bookingPromotion = BookingPromotion.Create(
                    bookingId: booking.Id,
                    promotionProgramId: promo.PromotionProgramId,
                    name: promo.PromotionName,
                    discountAmount: promo.DiscountAmount,
                    discountType: promo.DiscountType);

                booking.ApplyPromotion(bookingPromotion);

                appliedPromotions.Add(new AppliedPromotionDto(
                    promo.PromotionProgramId,
                    promo.PromotionName,
                    promo.DiscountType,
                    promo.DiscountAmount));
            }

            // 2d. Resolve free items from the promotion scan
            var freeItemIds = scanResult.FreeItems.Select(f => f.ConcessionId).Distinct().ToList();
            Dictionary<Guid, Concession> freeConcessionMap = new();
            if (freeItemIds.Count > 0)
            {
                var freeConcessions = await uow.Concessions.GetByIdsAsync(freeItemIds, ct);
                freeConcessionMap = freeConcessions.ToDictionary(c => c.Id);
            }

            foreach (var freeItem in scanResult.FreeItems)
            {
                if (freeConcessionMap.TryGetValue(freeItem.ConcessionId, out var concession))
                {
                    var freeConcession = BookingConcession.Create(
                        bookingId: booking.Id,
                        concessionId: freeItem.ConcessionId,
                        quantity: freeItem.Quantity,
                        isFree: true);
                    booking.Concessions.Add(freeConcession);
                    freeItems.Add(new FreeConcessionItemDto(
                        freeItem.ConcessionId,
                        concession.Name,
                        freeItem.Quantity));
                }
            }
        }

        // =============================================
        // 3. Coupon resolution and apply
        // =============================================

        decimal couponDiscountAmount = 0m;
        string? resolvedCouponCode = null;
        string? couponDescription = null;

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            // 3a. Normalize and validate coupon
            var code = couponCode.Trim().ToUpperInvariant();
            var now = DateTimeOffset.UtcNow;
            CustomerCoupon? customerCoupon = null;
            CouponTemplate? template = null;

            // 3b. Resolve personal coupon for registered customers
            if (customer?.IsRegistered == true)
            {
                customerCoupon = await uow.CustomerCoupons.GetByCustomerAndCodeAsync(customer.Id, code, ct);
            }

            if (customerCoupon is not null)
            {
                customerCoupon.ValidateAvailable(now);
            }
            else
            {
                // 3c. Fall back to public coupon template
                template = await uow.CouponTemplates.GetByCodeAsync(code, ct);
                if (template is null)
                    throw new InvalidOperationException("Mã giảm giá không hợp lệ.");

                template.ValidateAvailable(now);
            }

            // 3d. Compute discount based on coupon type and scope
            var discountType = customerCoupon?.DiscountType ?? template!.DiscountType;
            var discountValue = customerCoupon?.DiscountValue ?? template!.DiscountValue;
            var maxDiscountAmount = customerCoupon?.MaxDiscountAmount ?? template!.MaxDiscountAmount;
            var scope = customerCoupon?.Scope ?? template!.Scope;

            decimal qualifyingAmount = scope switch
            {
                DiscountScope.All => booking.OriginAmount,
                DiscountScope.Tickets => booking.Tickets.Sum(t => t.Ticket?.Price ?? 0m),
                DiscountScope.Concessions => booking.Concessions.Sum(c => (c.Concession?.Price ?? 0m) * c.Quantity),
                _ => booking.OriginAmount
            };

            decimal discount;
            if (discountType == DiscountType.Fixed)
            {
                discount = Math.Min(discountValue, qualifyingAmount);
            }
            else
            {
                discount = qualifyingAmount * discountValue / 100m;
                if (maxDiscountAmount.HasValue)
                    discount = Math.Min(discount, maxDiscountAmount.Value);
            }

            // 3e. Apply coupon to booking
            discount = Math.Round(discount, 2);
            couponDiscountAmount = discount;
            resolvedCouponCode = code;
            couponDescription = customerCoupon is not null
                ? $"Coupon {customerCoupon.CouponCode}"
                : template!.Description;

            booking.ApplyCoupon(code, discount);
        }

        // =============================================
        // 4. Loyalty and strategy discount composite
        // =============================================

        decimal totalDiscount = 0m;
        decimal ticketDiscount = 0m;
        decimal concessionDiscount = 0m;
        string? loyaltyTierName = null;
        string? loyaltyTierDescription = null;
        decimal? ticketDiscountPercent = null;
        decimal? concessionDiscountPercent = null;

        var effectiveCustomer = customer ?? new Customer { Id = Guid.Empty, Name = "Guest", IsRegistered = false };
        if (effectiveCustomer.IsRegistered || booking.AppliedPromotions.Count > 0)
        {
            // 4a. Run the strategy composite for all applicable discounts
            var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);
            var aggregator = new DiscountStrategyComposite(discountStrategies);
            totalDiscount = aggregator.CalculateTotalDiscount(booking, effectiveCustomer, activeTiers);

            // 4b. Resolve loyalty tier display info for registered customers
            if (customer?.IsRegistered == true)
            {
                var tierConfig = activeTiers.FirstOrDefault(t => t.Tier == customer.LoyaltyTier);
                if (tierConfig is not null)
                {
                    var result = loyaltyDiscountService.CalculateDiscount(
                        booking, customer, tierConfig);
                    ticketDiscount = result.TicketDiscount;
                    concessionDiscount = result.ConcessionDiscount;
                    loyaltyTierName = tierConfig.Name;
                    loyaltyTierDescription = tierConfig.Description;
                    ticketDiscountPercent = tierConfig.TicketDiscountPercent;
                    concessionDiscountPercent = tierConfig.ConcessionDiscountPercent;
                }
            }
        }

        // 5. Compute final amount (floor at zero)
        var finalAmount = Math.Max(0, booking.OriginAmount - totalDiscount);

        return new PricingResult(
            TotalDiscount: totalDiscount,
            FinalAmount: finalAmount,
            PromotionDiscountAmount: booking.TotalPromotionDiscount,
            CouponDiscountAmount: couponDiscountAmount,
            CouponCode: resolvedCouponCode,
            CouponDescription: couponDescription,
            AppliedPromotions: appliedPromotions,
            FreeItems: freeItems,
            TicketDiscount: ticketDiscount,
            ConcessionDiscount: concessionDiscount,
            LoyaltyTierName: loyaltyTierName,
            LoyaltyTierDescription: loyaltyTierDescription,
            TicketDiscountPercent: ticketDiscountPercent,
            ConcessionDiscountPercent: concessionDiscountPercent);
    }

    /// <summary>
    /// Builds the PromotionScanContext from booking inputs for the promotion scan engine.
    /// Extracts seat types, customer info, and monetary amounts.
    /// </summary>
    private static PromotionScanContext BuildPromotionScanContext(
        Customer? customer,
        ShowTime showTime,
        List<Ticket> selectedTickets,
        decimal originAmount,
        decimal ticketAmount,
        decimal concessionAmount)
    {
        var seatTypes = new List<string>();
        var seatIds = selectedTickets
            .Where(t => t.SeatId.HasValue)
            .Select(t => t.SeatId!.Value)
            .Distinct()
            .ToList();

        if (seatIds.Count > 0 && showTime.Screen?.Seats != null)
        {
            seatTypes = showTime.Screen.Seats
                .Where(s => seatIds.Contains(s.Id))
                .Select(s => s.Type.ToString())
                .Distinct()
                .ToList();
        }

        return new PromotionScanContext
        {
            CustomerId = customer?.Id,
            CustomerAge = customer?.DateOfBirth.HasValue == true
                ? (int)((DateTimeOffset.UtcNow - customer.DateOfBirth.Value).TotalDays / 365.25)
                : null,
            CustomerBirthMonth = customer?.DateOfBirth?.Month,
            CustomerGender = customer?.Gender,
            CustomerTier = customer?.LoyaltyTier.ToString(),
            BookingOriginAmount = originAmount,
            TicketAmount = ticketAmount,
            ConcessionAmount = concessionAmount,
            TicketCount = selectedTickets.Count,
            SeatTypes = seatTypes,
            ShowtimeFormat = showTime.Format.ToString()
        };
    }
}

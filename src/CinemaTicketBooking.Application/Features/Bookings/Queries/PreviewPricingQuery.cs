using CinemaTicketBooking.Domain.Services;
using Microsoft.EntityFrameworkCore;
using CinemaTicketBooking.Application.Features.Promotions;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Previews the final pricing breakdown (tickets + concessions + loyalty discount)
/// using the same domain services as the real booking creation.
/// Does NOT mutate any state.
/// </summary>
public class PreviewPricingQuery : IQuery<PreviewPricingResponse>
{
    public Guid ShowTimeId { get; set; }
    public List<Guid> SelectedTicketIds { get; set; } = [];
    public string CustomerSessionId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhoneNumber { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public List<CheckoutConcessionSelection> Concessions { get; set; } = [];
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Optional coupon code to apply in the preview.
    /// </summary>
    public string? CouponCode { get; set; }
}

/// <summary>
/// Handler that reuses the same discount-composition logic as CreateBookingHandler
/// but returns a detailed price breakdown without persisting anything.
/// </summary>
public class PreviewPricingHandler(
    IUnitOfWork uow,
    IEnumerable<IDiscountStrategy> discountStrategies,
    ILoyaltyDiscountService loyaltyDiscountService,
    IPromotionScanService promotionScanService)
{
    public async Task<PreviewPricingResponse> Handle(
        PreviewPricingQuery query,
        CancellationToken ct)
    {
        // 1. Load showtime with tickets.
        var showTime = await uow.ShowTimes.LoadFullAsync(query.ShowTimeId, ct)
            ?? throw new InvalidOperationException($"ShowTime with ID '{query.ShowTimeId}' was not found.");

        // 2. Resolve selected tickets.
        var selectedTickets = showTime.Tickets
            .Where(x => query.SelectedTicketIds.Contains(x.Id))
            .ToList();

        decimal ticketOrigin = selectedTickets.Sum(t => t.Price);

        // 3. Resolve selected concessions and compute concession origin.
        var resolvedConcessions = new List<(Concession Concession, int Quantity)>();
        decimal concessionOrigin = 0m;

        foreach (var sel in query.Concessions)
        {
            var concession = await uow.Concessions.GetByIdAsync(sel.ConcessionId, ct);
            if (concession is not null && concession.IsAvailable)
            {
                concessionOrigin += concession.Price * sel.Quantity;
                resolvedConcessions.Add((concession, sel.Quantity));
            }
        }

        decimal originAmount = ticketOrigin + concessionOrigin;

        // 4. Find customer (same resolution as CreateBookingHandler).
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(query.CustomerSessionId))
        {
            customer = await uow.Customers.GetTrackedBySessionIdAsync(query.CustomerSessionId, ct);
        }

        decimal ticketDiscount = 0m;
        decimal concessionDiscount = 0m;
        decimal couponDiscountAmount = 0m;
        string? loyaltyTierName = null;
        string? loyaltyTierDescription = null;
        decimal? ticketDiscountPercent = null;
        decimal? concessionDiscountPercent = null;
        string? resolvedCouponCode = null;
        string? couponDescription = null;

        // Build the temp booking once (used for all discount calculations)
        var tempBooking = BuildPreviewBooking(
            query, selectedTickets, resolvedConcessions, originAmount);

        // 5. Promotion scanning FIRST (populate AppliedPromotions so strategies can see them).
        var appliedPromotions = new List<Promotions.AppliedPromotionDto>();
        var freeItems = new List<Promotions.FreeConcessionItemDto>();

        var activePromotions = await uow.PromotionPrograms.GetActiveWithConditionsAsync(ct);
        if (activePromotions.Count > 0)
        {
            var scanContext = BuildPromotionScanContext(
                customer, showTime, selectedTickets, originAmount, ticketOrigin, concessionOrigin);

            var scanResult = await promotionScanService.ScanAsync(
                activePromotions.ToList(), scanContext, ct);

            // Populate tempBooking.AppliedPromotions so PromotionDiscountStrategy can read them.
            foreach (var promo in scanResult.AppliedPromotions)
            {
                tempBooking.AppliedPromotions.Add(BookingPromotion.Create(
                    bookingId: tempBooking.Id,
                    promotionProgramId: promo.PromotionProgramId,
                    name: promo.PromotionName,
                    discountAmount: promo.DiscountAmount,
                    discountType: promo.DiscountType));

                appliedPromotions.Add(new Promotions.AppliedPromotionDto(
                    promo.PromotionProgramId,
                    promo.PromotionName,
                    promo.DiscountType,
                    promo.DiscountAmount));
            }

            if (scanResult.FreeItems.Count > 0)
            {
                foreach (var freeItem in scanResult.FreeItems)
                {
                    var concession = await uow.Concessions.GetByIdAsync(freeItem.ConcessionId, ct);
                    freeItems.Add(new Promotions.FreeConcessionItemDto(
                        freeItem.ConcessionId,
                        concession?.Name ?? "Unknown",
                        freeItem.Quantity));
                }
            }
        }

        // 6. Handle coupon code if provided (BEFORE composite so CouponDiscountStrategy sees it).
        if (!string.IsNullOrWhiteSpace(query.CouponCode))
        {
            var (couponDiscount, couponCode, desc) = await ComputeCouponDiscountAsync(
                query.CouponCode, customer, tempBooking, ct);
            couponDiscountAmount = couponDiscount;
            resolvedCouponCode = couponCode;
            couponDescription = desc;
            // Populate tempBooking so CouponDiscountStrategy can read it.
            tempBooking.CouponCode = couponCode;
            tempBooking.CouponDiscountAmount = couponDiscount;
        }

        // 7. Run ALL discount strategies through unified composite (Loyalty + Coupon + Promotion).
        //    At this point tempBooking has CouponCode/CouponDiscountAmount and AppliedPromotions populated.
        //    For guest users with promotions, create a dummy customer so the composite can still run.
        var totalDiscount = 0m;
        var effectiveCustomer = customer ?? new Customer { Id = Guid.Empty, Name = "Guest", IsRegistered = false };
        if (effectiveCustomer.IsRegistered || tempBooking.AppliedPromotions.Count > 0)
        {
            var activeTiers = await uow.LoyaltyTiers.GetActiveTiersAsync(ct);
            var aggregator = new DiscountStrategyComposite(discountStrategies);
            totalDiscount = aggregator.CalculateTotalDiscount(tempBooking, effectiveCustomer, activeTiers);

            // Extract per-category loyalty details for display (only for registered customers).
            if (customer?.IsRegistered == true)
            {
                var tierConfig = activeTiers.FirstOrDefault(t => t.Tier == customer.LoyaltyTier);
                if (tierConfig is not null)
                {
                    var result = loyaltyDiscountService.CalculateDiscount(
                        tempBooking, customer, tierConfig);
                    ticketDiscount = result.TicketDiscount;
                    concessionDiscount = result.ConcessionDiscount;
                    loyaltyTierName = tierConfig.Name;
                    loyaltyTierDescription = tierConfig.Description;
                    ticketDiscountPercent = tierConfig.TicketDiscountPercent;
                    concessionDiscountPercent = tierConfig.ConcessionDiscountPercent;
                }
            }
        }

        return new PreviewPricingResponse(
            OriginAmount: originAmount,
            TicketDiscount: ticketDiscount,
            ConcessionDiscount: concessionDiscount,
            TotalDiscount: totalDiscount,
            FinalAmount: Math.Max(0, originAmount - totalDiscount),
            IsRegisteredCustomer: customer?.IsRegistered ?? false,
            LoyaltyTierName: loyaltyTierName,
            LoyaltyTierDescription: loyaltyTierDescription,
            TicketDiscountPercent: ticketDiscountPercent,
            ConcessionDiscountPercent: concessionDiscountPercent,
            CouponDiscountAmount: couponDiscountAmount,
            CouponCode: resolvedCouponCode,
            CouponDescription: couponDescription,
            PromotionDiscountAmount: tempBooking.TotalPromotionDiscount,
            AppliedPromotions: appliedPromotions,
            FreeItems: freeItems);
    }

    private async Task<(decimal Discount, string Code, string? Description)> ComputeCouponDiscountAsync(
        string couponCode,
        Customer? customer,
        Booking tempBooking,
        CancellationToken ct)
    {
        var code = couponCode.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;
        CustomerCoupon? customerCoupon = null;
        CouponTemplate? template = null;

        // Try personal coupon first
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
            // Try public template
            template = await uow.CouponTemplates.GetByCodeAsync(code, ct);
            if (template is null)
                throw new InvalidOperationException("Mã giảm giá không hợp lệ.");

            template.ValidateAvailable(now);
        }

        var discountType = customerCoupon?.DiscountType ?? template!.DiscountType;
        var discountValue = customerCoupon?.DiscountValue ?? template!.DiscountValue;
        var maxDiscountAmount = customerCoupon?.MaxDiscountAmount ?? template!.MaxDiscountAmount;
        var scope = customerCoupon?.Scope ?? template!.Scope;

        // Calculate qualifying amount
        decimal qualifyingAmount = scope switch
        {
            DiscountScope.All => tempBooking.OriginAmount,
            DiscountScope.Tickets => tempBooking.Tickets.Sum(t => t.Ticket?.Price ?? 0m),
            DiscountScope.Concessions => tempBooking.Concessions.Sum(c => (c.Concession?.Price ?? 0m) * c.Quantity),
            _ => tempBooking.OriginAmount
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

        discount = Math.Round(discount, 2);
        var description = customerCoupon is not null
            ? $"Coupon {customerCoupon.CouponCode}"
            : template!.Description;

        return (discount, code, description);
    }

    private static Booking BuildPreviewBooking(
        PreviewPricingQuery query,
        List<Ticket> selectedTickets,
        List<(Concession Concession, int Quantity)> resolvedConcessions,
        decimal originAmount)
    {
        var booking = new Booking
        {
            ShowTimeId = query.ShowTimeId,
            CustomerName = query.CustomerName,
            PhoneNumber = query.CustomerPhoneNumber,
            Email = query.CustomerEmail,
            Status = BookingStatus.Pending,
            OriginAmount = originAmount,
            FinalAmount = originAmount,
            Tickets = selectedTickets
                .Select(t => new BookingTicket
                {
                    Id = Guid.CreateVersion7(),
                    TicketId = t.Id,
                    Ticket = t
                })
                .ToList(),
            Concessions = resolvedConcessions
                .Select(rc => new BookingConcession
                {
                    Id = Guid.CreateVersion7(),
                    ConcessionId = rc.Concession.Id,
                    Concession = rc.Concession,
                    Quantity = rc.Quantity
                })
                .ToList()
        };

        return booking;
    }

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

/// <summary>
/// Validates the preview pricing request.
/// </summary>
public class PreviewPricingValidator : AbstractValidator<PreviewPricingQuery>
{
    public PreviewPricingValidator()
    {
        RuleFor(x => x.ShowTimeId)
            .NotEmpty()
            .WithMessage("ShowTime ID is required.");

        RuleFor(x => x.CustomerSessionId)
            .NotEmpty()
            .WithMessage("Customer session ID is required.")
            .MaximumLength(MaxLengthConsts.SessionId);

        RuleFor(x => x.SelectedTicketIds)
            .NotEmpty()
            .WithMessage("Selected ticket IDs are required.");
    }
}
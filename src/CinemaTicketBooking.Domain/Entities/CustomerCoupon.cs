namespace CinemaTicketBooking.Domain;

/// <summary>
/// A coupon instance issued to a specific customer.
/// Can come from a public template (redeemed by entering the code) or
/// a personal issuance (e.g., loyalty tier upgrade reward).
/// </summary>
public class CustomerCoupon : AggregateRoot
{
    /// <summary>The customer who owns this coupon.</summary>
    public Guid CustomerId { get; set; }

    /// <summary>Optional link to the template. Null for ad-hoc personal coupons.</summary>
    public Guid? CouponTemplateId { get; set; }

    /// <summary>The actual coupon code (denormalized from template or set for personal).</summary>
    public string CouponCode { get; set; } = string.Empty;

    /// <summary>Denormalized discount details for snapshot consistency at issuance time.</summary>
    public DiscountType DiscountType { get; set; } = DiscountType.Fixed;
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DiscountScope Scope { get; set; } = DiscountScope.All;

    /// <summary>How many times this coupon has been used. Personal coupons: max 1.</summary>
    public int UsageCount { get; set; }

    /// <summary>Max allowed usage. 1 for personal, based on template for public.</summary>
    public int MaxUsage { get; set; } = 1;

    /// <summary>Convenience flag — true when UsageCount >= MaxUsage.</summary>
    public bool IsUsed => UsageCount >= MaxUsage;

    /// <summary>When this coupon was issued.</summary>
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When this coupon was last used (null if never used).</summary>
    public DateTimeOffset? UsedAt { get; set; }

    /// <summary>When this coupon expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    // =============================================================
    // Factory
    // =============================================================

    /// <summary>
    /// Issues a new personal coupon from a CouponTemplate (for loyalty upgrades, admin grants).
    /// </summary>
    public static CustomerCoupon IssueFromTemplate(
        Guid customerId,
        CouponTemplate template,
        DateTimeOffset now)
    {
        if (template.Type != CouponType.Personal)
            throw new ArgumentException("Template must be of Personal type to issue a personal coupon.", nameof(template));

        if (!template.IsActive)
            throw new InvalidOperationException("Cannot issue from an inactive template.");

        return new CustomerCoupon
        {
            CustomerId = customerId,
            CouponTemplateId = template.Id,
            CouponCode = template.Code,
            DiscountType = template.DiscountType,
            DiscountValue = template.DiscountValue,
            MaxDiscountAmount = template.MaxDiscountAmount,
            Scope = template.Scope,
            UsageCount = 0,
            MaxUsage = 1,
            IssuedAt = now,
            ExpiresAt = template.ComputeExpiry(now)
        };
    }

    /// <summary>
    /// Records usage of a public coupon by a customer (creating a usage record).
    /// For public codes, the template handles global usage counting; this tracks per-customer usage.
    /// </summary>
    public static CustomerCoupon RedeemPublic(
        Guid customerId,
        CouponTemplate template,
        DateTimeOffset now)
    {
        if (template.Type != CouponType.Public)
            throw new ArgumentException("Template must be of Public type for public redemption.", nameof(template));

        return new CustomerCoupon
        {
            CustomerId = customerId,
            CouponTemplateId = template.Id,
            CouponCode = template.Code,
            DiscountType = template.DiscountType,
            DiscountValue = template.DiscountValue,
            MaxDiscountAmount = template.MaxDiscountAmount,
            Scope = template.Scope,
            UsageCount = 0,
            MaxUsage = template.MaxUsagePerUser > 0 ? template.MaxUsagePerUser : 1,
            IssuedAt = now,
            ExpiresAt = template.ComputeExpiry(now)
        };
    }

    /// <summary>
    /// Creates a personal coupon with custom details (admin ad-hoc issuance).
    /// </summary>
    public static CustomerCoupon CreatePersonal(
        Guid customerId,
        string couponCode,
        DiscountType discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        DiscountScope scope,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
            throw new ArgumentException("Coupon code cannot be empty.", nameof(couponCode));

        if (discountValue <= 0)
            throw new ArgumentException("Discount value must be positive.", nameof(discountValue));

        return new CustomerCoupon
        {
            CustomerId = customerId,
            CouponTemplateId = null,
            CouponCode = couponCode.Trim().ToUpperInvariant(),
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            Scope = scope,
            UsageCount = 0,
            MaxUsage = 1,
            IssuedAt = now,
            ExpiresAt = expiresAt
        };
    }

    // =============================================================
    // Mutators
    // =============================================================

    /// <summary>
    /// Marks this coupon as used. Throws if already exhausted.
    /// </summary>
    public void MarkUsed(DateTimeOffset now)
    {
        if (IsUsed)
            throw new InvalidOperationException("Coupon đã được sử dụng hết.");

        if (now > ExpiresAt)
            throw new InvalidOperationException("Coupon đã hết hạn.");

        UsageCount++;
        UsedAt = now;
    }

    /// <summary>
    /// Validates the coupon is still usable (not expired, not used up).
    /// Throws InvalidOperationException with descriptive error message.
    /// </summary>
    public void ValidateAvailable(DateTimeOffset now)
    {
        if (IsUsed)
            throw new InvalidOperationException("Coupon đã được sử dụng hết.");

        if (now > ExpiresAt)
            throw new InvalidOperationException("Coupon đã hết hạn.");
    }
}
namespace CinemaTicketBooking.Domain;

/// <summary>
/// Defines a coupon "blueprint" — the template from which customer coupons are issued
/// or against which public coupon codes are validated. Admin-managed via MVC views.
/// </summary>
public class CouponTemplate : AggregateRoot
{
    /// <summary>
    /// The coupon code (unique for public codes; auto-generated or manually set).
    /// Personal templates may leave this empty since each issue gets its own code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Public (KOL / shared) or Personal (one-time per customer, from upgrade or admin).</summary>
    public CouponType Type { get; set; } = CouponType.Public;

    /// <summary>Fixed amount (VND) or Percentage.</summary>
    public DiscountType DiscountType { get; set; } = DiscountType.Fixed;

    /// <summary>Discount value. If Fixed, in VND. If Percentage, 0–100.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>
    /// For Percentage discounts: the maximum discounted amount in VND.
    /// Null = no cap. Ignored for Fixed discounts.
    /// </summary>
    public decimal? MaxDiscountAmount { get; set; }

    /// <summary>Which items the discount applies to.</summary>
    public DiscountScope Scope { get; set; } = DiscountScope.All;

    /// <summary>How many times this template can be used across all customers. 0 = unlimited. Only for Public type.</summary>
    public int MaxUsageCount { get; set; }

    /// <summary>How many times a single customer can use this coupon. 0 = unlimited. Only for Public type.</summary>
    public int MaxUsagePerUser { get; set; } = 1;

    /// <summary>
    /// Validity duration in days from the moment the coupon is issued (for Personal type)
    /// or from the current date (for Public type).
    /// </summary>
    public int DurationDays { get; set; } = 30;

    /// <summary>Whether this template is active (admin toggle).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display description / terms.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Total usage count across all customers (denormalized for performance).</summary>
    public int TotalUsedCount { get; set; }

    // =============================================================
    // Factory
    // =============================================================

    /// <summary>
    /// Creates a new coupon template with full validation.
    /// </summary>
    public static CouponTemplate Create(
        string code,
        CouponType type,
        DiscountType discountType,
        decimal discountValue,
        decimal? maxDiscountAmount,
        DiscountScope scope,
        int maxUsageCount,
        int maxUsagePerUser,
        int durationDays,
        string description,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty.", nameof(code));

        if (discountValue <= 0)
            throw new ArgumentException("Discount value must be positive.", nameof(discountValue));

        if (discountType == DiscountType.Percentage && (discountValue > 100 || discountValue <= 0))
            throw new ArgumentException("Percentage discount must be between 0 and 100.", nameof(discountValue));

        if (maxDiscountAmount.HasValue && discountType == DiscountType.Fixed)
            throw new ArgumentException("MaxDiscountAmount is only valid for Percentage discounts.", nameof(maxDiscountAmount));

        if (durationDays <= 0)
            throw new ArgumentException("DurationDays must be positive.", nameof(durationDays));

        if (maxUsageCount < 0)
            throw new ArgumentException("MaxUsageCount cannot be negative.", nameof(maxUsageCount));

        if (maxUsagePerUser < 0)
            throw new ArgumentException("MaxUsagePerUser cannot be negative.", nameof(maxUsagePerUser));

        return new CouponTemplate
        {
            Code = code.Trim().ToUpperInvariant(),
            Type = type,
            DiscountType = discountType,
            DiscountValue = discountValue,
            MaxDiscountAmount = maxDiscountAmount,
            Scope = scope,
            MaxUsageCount = maxUsageCount,
            MaxUsagePerUser = maxUsagePerUser,
            DurationDays = durationDays,
            Description = description,
            IsActive = isActive,
            TotalUsedCount = 0
        };
    }

    // =============================================================
    // Mutators
    // =============================================================

    /// <summary>Activates the template.</summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>Deactivates the template (prevents further usage).</summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>Increments total usage counter.</summary>
    public void IncrementUsage()
    {
        if (MaxUsageCount > 0 && TotalUsedCount >= MaxUsageCount)
            throw new InvalidOperationException("Coupon template has reached its maximum usage count.");

        TotalUsedCount++;
    }

    /// <summary>
    /// Validates whether this template can currently be used.
    /// Checks IsActive and usage count. (Date validity is computed at issuance time via DurationDays.)
    /// Throws InvalidOperationException with a descriptive message when validation fails.
    /// </summary>
    public void ValidateAvailable(DateTimeOffset now)
    {
        if (!IsActive)
            throw new InvalidOperationException("Mã giảm giá đã bị vô hiệu hóa.");

        if (MaxUsageCount > 0 && TotalUsedCount >= MaxUsageCount)
            throw new InvalidOperationException("Mã giảm giá đã hết lượt sử dụng.");
    }

    /// <summary>
    /// Computes the expiry date/time from the given reference point using DurationDays.
    /// </summary>
    public DateTimeOffset ComputeExpiry(DateTimeOffset from) => from.AddDays(DurationDays);

    /// <summary>
    /// Updates configurable fields (admin edit).
    /// </summary>
    public void UpdateConfig(
        decimal discountValue,
        decimal? maxDiscountAmount,
        DiscountScope scope,
        int maxUsageCount,
        int maxUsagePerUser,
        int durationDays,
        string description,
        bool isActive)
    {
        if (discountValue <= 0)
            throw new ArgumentException("Discount value must be positive.", nameof(discountValue));

        if (DiscountType == DiscountType.Percentage && (discountValue > 100 || discountValue <= 0))
            throw new ArgumentException("Percentage discount must be between 0 and 100.", nameof(discountValue));

        if (durationDays <= 0)
            throw new ArgumentException("DurationDays must be positive.", nameof(durationDays));

        DiscountValue = discountValue;
        MaxDiscountAmount = maxDiscountAmount;
        Scope = scope;
        MaxUsageCount = maxUsageCount;
        MaxUsagePerUser = maxUsagePerUser;
        DurationDays = durationDays;
        Description = description;
        IsActive = isActive;
    }
}
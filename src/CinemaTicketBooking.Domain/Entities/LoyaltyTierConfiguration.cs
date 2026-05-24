namespace CinemaTicketBooking.Domain;

/// <summary>
/// Admin-configurable configuration for a loyalty tier.
/// Defines point thresholds and discount percentages for tickets and concessions.
/// Seeded once at infrastructure startup; admin can only update, not create or delete.
/// </summary>
public class LoyaltyTierConfiguration : AggregateRoot
{
    /// <summary>Tier level (unique).</summary>
    public LoyaltyTier Tier { get; set; }

    /// <summary>Display name (e.g., "Bạc", "Vàng").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Lower bound of accumulated points for this tier (inclusive).</summary>
    public int MinPoints { get; set; }

    /// <summary>Upper bound of accumulated points for this tier (inclusive). Null for Ruby (max tier).</summary>
    public int? MaxPoints { get; set; }

    /// <summary>Discount percentage applied to tickets (e.g., 5.0 = 5%).</summary>
    public decimal TicketDiscountPercent { get; set; }

    /// <summary>Discount percentage applied to concessions (e.g., 10.0 = 10%).</summary>
    public decimal ConcessionDiscountPercent { get; set; }

    /// <summary>Optional description displayed in admin and customer-facing UI.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Whether this tier configuration is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional reference to a <see cref="CouponTemplate"/> of Personal type.
    /// When a customer upgrades to this tier, a personal coupon will be issued from this template.
    /// </summary>
    public Guid? CouponTemplateId { get; set; }
    public CouponTemplate? CouponTemplate { get; set; }

    // =============================================================
    // Factory
    // =============================================================

    /// <summary>
    /// Creates a new tier configuration. Used during infrastructure data seeding.
    /// </summary>
    public static LoyaltyTierConfiguration Create(
        LoyaltyTier tier,
        string name,
        int minPoints,
        int? maxPoints,
        decimal ticketDiscountPercent,
        decimal concessionDiscountPercent,
        string description = "",
        bool isActive = true)
    {
        if (ticketDiscountPercent < 0 || ticketDiscountPercent > 100)
            throw new ArgumentException("Ticket discount percent must be between 0 and 100.", nameof(ticketDiscountPercent));

        if (concessionDiscountPercent < 0 || concessionDiscountPercent > 100)
            throw new ArgumentException("Concession discount percent must be between 0 and 100.", nameof(concessionDiscountPercent));

        if (minPoints < 0)
            throw new ArgumentException("Min points cannot be negative.", nameof(minPoints));

        if (maxPoints.HasValue && maxPoints.Value < minPoints)
            throw new ArgumentException("Max points must be >= min points.", nameof(maxPoints));

        return new LoyaltyTierConfiguration
        {
            Tier = tier,
            Name = name,
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            TicketDiscountPercent = ticketDiscountPercent,
            ConcessionDiscountPercent = concessionDiscountPercent,
            Description = description,
            IsActive = isActive
        };
    }

    // =============================================================
    // Mutators
    // =============================================================

    /// <summary>
    /// Updates all configurable fields and raises a configuration-updated event.
    /// </summary>
    public void UpdateConfig(
        string name,
        int minPoints,
        int? maxPoints,
        decimal ticketDiscountPercent,
        decimal concessionDiscountPercent,
        string description,
        bool isActive,
        Guid? couponTemplateId = null)
    {
        if (ticketDiscountPercent < 0 || ticketDiscountPercent > 100)
            throw new ArgumentException("Ticket discount percent must be between 0 and 100.", nameof(ticketDiscountPercent));

        if (concessionDiscountPercent < 0 || concessionDiscountPercent > 100)
            throw new ArgumentException("Concession discount percent must be between 0 and 100.", nameof(concessionDiscountPercent));

        if (minPoints < 0)
            throw new ArgumentException("Min points cannot be negative.", nameof(minPoints));

        if (maxPoints.HasValue && maxPoints.Value < minPoints)
            throw new ArgumentException("Max points must be >= min points.", nameof(maxPoints));

        Name = name;
        MinPoints = minPoints;
        MaxPoints = maxPoints;
        TicketDiscountPercent = ticketDiscountPercent;
        ConcessionDiscountPercent = concessionDiscountPercent;
        Description = description;
        IsActive = isActive;
        CouponTemplateId = couponTemplateId;

        RaiseEvent(new LoyaltyTierConfigurationUpdated(Id, Tier));
    }
}

namespace CinemaTicketBooking.Domain;

/// <summary>
/// Raised when a customer earns loyalty points from a confirmed booking.
/// </summary>
public record LoyaltyPointsEarned(
    Guid CustomerId,
    int PointsEarned,
    int TotalPoints,
    Guid BookingId,
    decimal BookingAmount) : BaseDomainEvent;

/// <summary>
/// Raised when a customer's loyalty tier is upgraded.
/// Side effects: send congratulation email, invalidate loyalty cache.
/// </summary>
public record LoyaltyTierUpgraded(
    Guid CustomerId,
    LoyaltyTier PreviousTier,
    LoyaltyTier NewTier,
    int TotalPoints) : BaseDomainEvent;

/// <summary>
/// Raised when an admin updates a loyalty tier configuration.
/// Side effects: invalidate tier list cache.
/// </summary>
public record LoyaltyTierConfigurationUpdated(
    Guid ConfigurationId,
    LoyaltyTier Tier) : BaseDomainEvent;

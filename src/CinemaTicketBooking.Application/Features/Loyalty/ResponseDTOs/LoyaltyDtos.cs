namespace CinemaTicketBooking.Application.Features.Loyalty;

/// <summary>
/// DTO for a loyalty tier configuration.
/// </summary>
public record LoyaltyTierDto(
    Guid Id,
    Domain.LoyaltyTier Tier,
    string Name,
    int MinPoints,
    int? MaxPoints,
    decimal TicketDiscountPercent,
    decimal ConcessionDiscountPercent,
    string Description,
    bool IsActive);

/// <summary>
/// DTO for a customer's current loyalty status.
/// </summary>
public record CustomerLoyaltyDto(
    Guid CustomerId,
    Domain.LoyaltyTier CurrentTier,
    string TierName,
    int AccumulatedPoints,
    int? PointsToNextTier,
    decimal TicketDiscountPercent,
    decimal ConcessionDiscountPercent,
    Domain.LoyaltyTier? NextTier);

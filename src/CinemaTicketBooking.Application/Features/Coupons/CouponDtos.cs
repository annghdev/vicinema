namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// DTO for a coupon template (admin-facing).
/// </summary>
public record CouponTemplateDto(
    Guid Id,
    string Code,
    string Type,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    string Scope,
    int MaxUsageCount,
    int MaxUsagePerUser,
    int DurationDays,
    int TotalUsedCount,
    bool IsActive,
    string Description,
    DateTimeOffset CreatedAt);

/// <summary>
/// DTO for a customer's coupon (customer-facing).
/// </summary>
public record CustomerCouponDto(
    Guid Id,
    Guid CustomerId,
    string CouponCode,
    string DiscountType,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    string Scope,
    int UsageCount,
    int MaxUsage,
    bool IsUsed,
    DateTimeOffset IssuedAt,
    DateTimeOffset? UsedAt,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Result of verifying a coupon code before applying it to a booking.
/// </summary>
public record CouponValidationResultDto(
    bool IsValid,
    string? ErrorMessage,
    string? CouponCode,
    string? DiscountType,
    decimal? DiscountValue,
    decimal? MaxDiscountAmount,
    string? Scope,
    string? Description);
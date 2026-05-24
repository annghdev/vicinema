using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Domain.Services;

public record PromotionScanContext
{
    public Guid? CustomerId { get; init; }
    public int? CustomerAge { get; init; }
    public int? CustomerBirthMonth { get; init; }
    public string? CustomerGender { get; init; }
    public string? CustomerTier { get; init; }
    public decimal BookingOriginAmount { get; init; }
    public decimal TicketAmount { get; init; }
    public decimal ConcessionAmount { get; init; }
    public int TicketCount { get; init; }
    public List<string> SeatTypes { get; init; } = [];
    public string? ShowtimeFormat { get; init; }
}

public record PromotionScanResult
{
    public decimal TotalDiscount { get; init; }
    public List<AppliedPromotionInfo> AppliedPromotions { get; init; } = [];
    public List<FreeItemInfo> FreeItems { get; init; } = [];
}

public record AppliedPromotionInfo
{
    public Guid PromotionProgramId { get; init; }
    public string PromotionName { get; init; } = string.Empty;
    public PromotionDiscountType DiscountType { get; init; }
    public decimal DiscountAmount { get; init; }
}

public record FreeItemInfo
{
    public Guid ConcessionId { get; init; }
    public string ConcessionName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

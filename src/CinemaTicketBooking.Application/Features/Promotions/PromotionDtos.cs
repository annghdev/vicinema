using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Application.Features.Promotions;

public record PromotionProgramDto(
    Guid Id,
    string Name,
    string? Description,
    string? PosterImage,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsActive,
    string DiscountType,
    string? DiscountForm,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MaxDiscountPercentage,
    int? MaxUsagePerCustomer,
    int ConditionCount,
    DateTimeOffset CreatedAt);

public record PromotionProgramDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? PosterImage,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    bool IsActive,
    bool IsDeleted,
    string DiscountType,
    string? DiscountForm,
    decimal DiscountValue,
    decimal? MaxDiscountAmount,
    decimal? MaxDiscountPercentage,
    int? MaxUsagePerCustomer,
    List<PromotionConditionDto> Conditions,
    List<PromotionFreeConcessionItemDto> FreeConcessionItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record PromotionConditionDto(
    Guid? Id,
    string ConditionType,
    string? Value,
    string? SecondaryValue);

public record PromotionFreeConcessionItemDto(
    Guid? Id,
    Guid ConcessionId,
    int Quantity);

public record AppliedPromotionDto(
    Guid PromotionProgramId,
    string PromotionName,
    PromotionDiscountType DiscountType,
    decimal DiscountAmount);

public record FreeConcessionItemDto(
    Guid ConcessionId,
    string ConcessionName,
    int Quantity);

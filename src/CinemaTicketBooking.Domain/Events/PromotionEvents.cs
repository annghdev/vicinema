using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Domain.Events;

public record PromotionCreated(Guid PromotionProgramId) : BaseDomainEvent;
public record PromotionUpdated(Guid PromotionProgramId) : BaseDomainEvent;
public record PromotionDeleted(Guid PromotionProgramId) : BaseDomainEvent;
public record PromotionActivated(Guid PromotionProgramId) : BaseDomainEvent;
public record PromotionDeactivated(Guid PromotionProgramId) : BaseDomainEvent;
public record PromotionApplied(
    Guid PromotionProgramId,
    Guid BookingId,
    Guid? CustomerId,
    string PromotionName,
    decimal DiscountAmount,
    PromotionDiscountType DiscountType) : BaseDomainEvent;

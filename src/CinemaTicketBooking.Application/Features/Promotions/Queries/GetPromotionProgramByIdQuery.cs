using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features.Promotions;

public class GetPromotionProgramByIdQuery : ICachableQuery<PromotionProgramDetailDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PromotionCacheKeys.Detail(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

public class GetPromotionProgramByIdHandler(IUnitOfWork uow)
{
    public async Task<PromotionProgramDetailDto?> Handle(GetPromotionProgramByIdQuery query, CancellationToken ct)
    {
        var promotion = await uow.PromotionPrograms.GetByIdWithConditionsAsync(query.Id, ct);
        if (promotion is null)
            return null;

        return new PromotionProgramDetailDto(
            Id: promotion.Id,
            Name: promotion.Name,
            Description: promotion.Description,
            PosterImage: promotion.PosterImage,
            StartDate: promotion.StartDate,
            EndDate: promotion.EndDate,
            IsActive: promotion.IsActive,
            IsDeleted: promotion.DeletedAt.HasValue,
            DiscountType: promotion.DiscountType.ToString(),
            DiscountForm: promotion.DiscountForm.HasValue ? promotion.DiscountForm.Value.ToString() : null,
            DiscountValue: promotion.DiscountValue,
            MaxDiscountAmount: promotion.MaxDiscountAmount,
            MaxDiscountPercentage: promotion.MaxDiscountPercentage,
            MaxUsagePerCustomer: promotion.MaxUsagePerCustomer,
            Conditions: promotion.Conditions
                .Select(c => new PromotionConditionDto(
                    c.Id,
                    c.ConditionType.ToString(),
                    c.Value,
                    c.SecondaryValue))
                .ToList(),
            FreeConcessionItems: promotion.FreeConcessionItems?
                .Select(f => new PromotionFreeConcessionItemDto(
                    f.Id,
                    f.ConcessionId,
                    f.Quantity))
                .ToList() ?? [],
            CreatedAt: promotion.CreatedAt,
            UpdatedAt: promotion.UpdatedAt);
    }
}

public class GetPromotionProgramByIdValidator : AbstractValidator<GetPromotionProgramByIdQuery>
{
    public GetPromotionProgramByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required.");
    }
}

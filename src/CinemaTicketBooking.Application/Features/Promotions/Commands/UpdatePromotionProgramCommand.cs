using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Application.Features.Promotions;

public class UpdatePromotionProgramCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PosterImage { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public string? DiscountForm { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MaxDiscountPercentage { get; set; }
    public int? MaxUsagePerCustomer { get; set; }
    public List<PromotionConditionDto> Conditions { get; set; } = [];
    public List<PromotionFreeConcessionItemDto> FreeConcessionItems { get; set; } = [];
}

public class UpdatePromotionProgramValidator : AbstractValidator<UpdatePromotionProgramCommand>
{
    public UpdatePromotionProgramValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(MaxLengthConsts.Name);

        RuleFor(x => x.Description)
            .MaximumLength(MaxLengthConsts.Description)
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.PosterImage)
            .MaximumLength(MaxLengthConsts.Url)
            .When(x => !string.IsNullOrEmpty(x.PosterImage));

        RuleFor(x => x.StartDate)
            .LessThan(x => x.EndDate)
            .WithMessage("Start date must be before end date.");

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0)
            .When(x => !string.Equals(x.DiscountType, "FreeConcession", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Discount value must be greater than zero.");

        RuleFor(x => x.DiscountType)
            .NotEmpty()
            .Must(t => Enum.TryParse<PromotionDiscountType>(t, ignoreCase: true, out _))
            .WithMessage("Invalid discount type.");

        RuleFor(x => x.FreeConcessionItems)
            .NotEmpty()
            .When(x => string.Equals(x.DiscountType, "FreeConcession", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Free concession items are required when discount type is FreeConcession.");

        RuleFor(x => x.DiscountForm)
            .NotEmpty()
            .When(x => !string.Equals(x.DiscountType, "FreeConcession", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Discount form is required when discount type is not FreeConcession.");

        RuleFor(x => x.DiscountForm)
            .Must(f => f == null || Enum.TryParse<DiscountForm>(f, ignoreCase: true, out _))
            .WithMessage("Invalid discount form.");

        RuleForEach(x => x.Conditions)
            .ChildRules(condition =>
            {
                condition.RuleFor(x => x.ConditionType)
                    .NotEmpty()
                    .Must(t => Enum.TryParse<ConditionType>(t, ignoreCase: true, out _))
                    .WithMessage("Invalid condition type.");
            });

        RuleForEach(x => x.FreeConcessionItems)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ConcessionId)
                    .NotEmpty()
                    .WithMessage("Concession ID is required.");
                item.RuleFor(x => x.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero.");
            });
    }
}

public class UpdatePromotionProgramHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task Handle(UpdatePromotionProgramCommand command, CancellationToken ct)
    {
        var promotion = await uow.PromotionPrograms.GetByIdWithConditionsAsync(command.Id, ct)
            ?? throw new InvalidOperationException("Promotion program not found.");

        var discountType = Enum.Parse<PromotionDiscountType>(command.DiscountType, ignoreCase: true);
        DiscountForm? discountForm = null;
        if (!string.IsNullOrEmpty(command.DiscountForm))
            discountForm = Enum.Parse<DiscountForm>(command.DiscountForm, ignoreCase: true);

        promotion.UpdateBasicInfo(
            name: command.Name,
            description: command.Description,
            posterImage: command.PosterImage,
            startDate: command.StartDate,
            endDate: command.EndDate,
            discountType: discountType,
            discountForm: discountForm,
            discountValue: command.DiscountValue,
            maxDiscountAmount: command.MaxDiscountAmount,
            maxDiscountPercentage: command.MaxDiscountPercentage,
            maxUsagePerCustomer: command.MaxUsagePerCustomer);

        promotion.Conditions.Clear();
        foreach (var c in command.Conditions)
        {
            var conditionType = Enum.Parse<ConditionType>(c.ConditionType, ignoreCase: true);
            promotion.AddCondition(PromotionCondition.Create(
                promotionProgramId: promotion.Id,
                conditionType: conditionType,
                value: c.Value,
                secondaryValue: c.SecondaryValue));
        }

        promotion.FreeConcessionItems?.Clear();
        if (discountType == PromotionDiscountType.FreeConcession)
        {
            foreach (var item in command.FreeConcessionItems)
            {
                promotion.AddFreeConcessionItem(PromotionFreeConcessionItem.Create(
                    promotionProgramId: promotion.Id,
                    concessionId: item.ConcessionId,
                    quantity: item.Quantity));
            }
        }

        uow.PromotionPrograms.Update(promotion);
        await uow.CommitAsync(ct);

        await cache.RemoveByPrefix(PromotionCacheKeys.ListPrefix, ct);
        await cache.RemoveAsync(PromotionCacheKeys.ActiveKey, ct);
        await cache.RemoveAsync(PromotionCacheKeys.Detail(promotion.Id), ct);
    }
}

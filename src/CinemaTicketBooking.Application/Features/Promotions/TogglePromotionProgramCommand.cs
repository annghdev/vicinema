using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Promotions;

public class TogglePromotionProgramCommand : ICommand
{
    public string CorrelationId { get; set; } = string.Empty;
    public Guid Id { get; set; }
}

public class TogglePromotionProgramValidator : AbstractValidator<TogglePromotionProgramCommand>
{
    public TogglePromotionProgramValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("ID is required.");
    }
}

public class TogglePromotionProgramHandler(
    IUnitOfWork uow,
    ICacheService cache)
{
    public async Task Handle(TogglePromotionProgramCommand command, CancellationToken ct)
    {
        var promotion = await uow.PromotionPrograms.GetByIdAsync(command.Id, ct)
            ?? throw new InvalidOperationException("Promotion program not found.");

        promotion.Toggle();
        uow.PromotionPrograms.Update(promotion);
        await uow.CommitAsync(ct);

        await cache.RemoveByPrefix(PromotionCacheKeys.ListPrefix, ct);
        await cache.RemoveAsync(PromotionCacheKeys.ActiveKey, ct);
        await cache.RemoveAsync(PromotionCacheKeys.Detail(promotion.Id), ct);
    }
}

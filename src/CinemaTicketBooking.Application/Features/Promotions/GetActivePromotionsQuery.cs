using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Promotions;

public class GetActivePromotionsQuery : ICachableQuery<IReadOnlyList<PromotionProgramDto>>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => PromotionCacheKeys.ActiveKey;
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

public class GetActivePromotionsHandler(IUnitOfWork uow)
{
    public async Task<IReadOnlyList<PromotionProgramDto>> Handle(GetActivePromotionsQuery _, CancellationToken ct)
    {
        var promotions = await uow.PromotionPrograms.GetActiveWithConditionsAsync(ct);

        return promotions
            .Select(p => new PromotionProgramDto(
                Id: p.Id,
                Name: p.Name,
                Description: p.Description,
                PosterImage: p.PosterImage,
                StartDate: p.StartDate,
                EndDate: p.EndDate,
                IsActive: p.IsActive,
                DiscountType: p.DiscountType.ToString(),
                DiscountForm: p.DiscountForm.HasValue ? p.DiscountForm.Value.ToString() : null,
                DiscountValue: p.DiscountValue,
                MaxDiscountAmount: p.MaxDiscountAmount,
                MaxDiscountPercentage: p.MaxDiscountPercentage,
                MaxUsagePerCustomer: p.MaxUsagePerCustomer,
                ConditionCount: p.Conditions.Count,
                CreatedAt: p.CreatedAt))
            .ToList() as IReadOnlyList<PromotionProgramDto>;
    }
}

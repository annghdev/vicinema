using CinemaTicketBooking.Domain.Enums;

namespace CinemaTicketBooking.Domain.Services;

public interface IPromotionScanService
{
    Task<PromotionScanResult> ScanAsync(
        List<PromotionProgram> activePromotions,
        PromotionScanContext context,
        CancellationToken ct = default);
}

public class PromotionScanService : IPromotionScanService
{
    public Task<PromotionScanResult> ScanAsync(
        List<PromotionProgram> activePromotions,
        PromotionScanContext context,
        CancellationToken ct = default)
    {
        var matchedPromotions = new List<PromotionProgram>();

        foreach (var promotion in activePromotions)
        {
            if (promotion.Conditions.Count == 0)
            {
                matchedPromotions.Add(promotion);
                continue;
            }

            bool allConditionsMet = true;
            foreach (var condition in promotion.Conditions)
            {
                if (!EvaluateCondition(condition, context))
                {
                    allConditionsMet = false;
                    break;
                }
            }

            if (allConditionsMet)
                matchedPromotions.Add(promotion);
        }

        var appliedPromotions = new List<AppliedPromotionInfo>();
        var freeItems = new List<FreeItemInfo>();

        var grouped = matchedPromotions
            .GroupBy(p => p.DiscountType)
            .ToList();

        foreach (var group in grouped)
        {
            if (group.Key == PromotionDiscountType.FreeConcession)
            {
                var allFreeItems = group
                    .SelectMany(p => p.FreeConcessionItems ?? [])
                    .GroupBy(i => i.ConcessionId)
                    .Select(g => new { ConcessionId = g.Key, Quantity = g.Max(i => i.Quantity) })
                    .ToList();

                foreach (var item in allFreeItems)
                {
                    freeItems.Add(new FreeItemInfo
                    {
                        ConcessionId = item.ConcessionId,
                        Quantity = item.Quantity
                    });
                }

                foreach (var freePromo in group)
                {
                    appliedPromotions.Add(new AppliedPromotionInfo
                    {
                        PromotionProgramId = freePromo.Id,
                        PromotionName = freePromo.Name,
                        DiscountType = freePromo.DiscountType,
                        DiscountAmount = 0
                    });
                }
            }
            else
            {
                var targetAmount = group.Key switch
                {
                    PromotionDiscountType.TicketOnly => context.TicketAmount,
                    PromotionDiscountType.Concession => context.ConcessionAmount,
                    _ => context.BookingOriginAmount
                };

                var best = group
                    .Select(p => new { Promotion = p, EffectiveDiscount = CalculateEffectiveDiscount(p, targetAmount) })
                    .OrderByDescending(x => x.EffectiveDiscount)
                    .First();

                appliedPromotions.Add(new AppliedPromotionInfo
                {
                    PromotionProgramId = best.Promotion.Id,
                    PromotionName = best.Promotion.Name,
                    DiscountType = best.Promotion.DiscountType,
                    DiscountAmount = best.EffectiveDiscount
                });
            }
        }

        var totalDiscount = appliedPromotions.Sum(p => p.DiscountAmount);

        return Task.FromResult(new PromotionScanResult
        {
            TotalDiscount = Math.Min(totalDiscount, context.BookingOriginAmount),
            AppliedPromotions = appliedPromotions,
            FreeItems = freeItems
        });
    }

    private static bool EvaluateCondition(PromotionCondition condition, PromotionScanContext context)
    {
        return condition.ConditionType switch
        {
            ConditionType.Age => EvaluateAge(condition, context),
            ConditionType.BirthdayMonth => EvaluateBirthdayMonth(condition, context),
            ConditionType.MinOrderAmount => EvaluateMinOrderAmount(condition, context),
            ConditionType.MinTickets => EvaluateMinTickets(condition, context),
            ConditionType.SeatType => EvaluateSeatType(condition, context),
            ConditionType.Gender => EvaluateGender(condition, context),
            ConditionType.Format => EvaluateFormat(condition, context),
            ConditionType.CustomerTier => EvaluateCustomerTier(condition, context),
            _ => false
        };
    }

    private static bool EvaluateAge(PromotionCondition condition, PromotionScanContext context)
    {
        if (context.CustomerAge == null) return false;
        var minAge = int.TryParse(condition.Value, out var min) ? min : 0;
        var maxAge = int.TryParse(condition.SecondaryValue, out var max) ? max : int.MaxValue;
        return context.CustomerAge >= minAge && context.CustomerAge <= maxAge;
    }

    private static bool EvaluateBirthdayMonth(PromotionCondition condition, PromotionScanContext context)
    {
        if (context.CustomerBirthMonth == null) return false;
        var month = int.TryParse(condition.Value, out var m) ? m : 0;
        return context.CustomerBirthMonth == month;
    }

    private static bool EvaluateMinOrderAmount(PromotionCondition condition, PromotionScanContext context)
    {
        var minAmount = decimal.TryParse(condition.Value, out var amount) ? amount : 0;
        return context.BookingOriginAmount >= minAmount;
    }

    private static bool EvaluateMinTickets(PromotionCondition condition, PromotionScanContext context)
    {
        var minTickets = int.TryParse(condition.Value, out var count) ? count : 0;
        return context.TicketCount >= minTickets;
    }

    private static bool EvaluateSeatType(PromotionCondition condition, PromotionScanContext context)
    {
        var seatType = condition.Value ?? string.Empty;
        return context.SeatTypes.Any(s => s.Equals(seatType, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EvaluateGender(PromotionCondition condition, PromotionScanContext context)
    {
        var gender = condition.Value ?? string.Empty;
        return string.Equals(context.CustomerGender, gender, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateFormat(PromotionCondition condition, PromotionScanContext context)
    {
        var format = condition.Value ?? string.Empty;
        return string.Equals(context.ShowtimeFormat, format, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateCustomerTier(PromotionCondition condition, PromotionScanContext context)
    {
        var tier = condition.Value ?? string.Empty;
        return string.Equals(context.CustomerTier, tier, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal CalculateEffectiveDiscount(PromotionProgram promotion, decimal targetAmount)
    {
        if (promotion.DiscountForm == DiscountForm.Fixed)
        {
            var discount = promotion.DiscountValue;
            if (promotion.MaxDiscountPercentage.HasValue)
            {
                var maxByPercentage = targetAmount * promotion.MaxDiscountPercentage.Value / 100m;
                discount = Math.Min(discount, maxByPercentage);
            }
            return Math.Min(discount, targetAmount);
        }
        else
        {
            var discount = targetAmount * promotion.DiscountValue / 100m;
            if (promotion.MaxDiscountAmount.HasValue)
            {
                discount = Math.Min(discount, promotion.MaxDiscountAmount.Value);
            }
            return Math.Min(discount, targetAmount);
        }
    }
}

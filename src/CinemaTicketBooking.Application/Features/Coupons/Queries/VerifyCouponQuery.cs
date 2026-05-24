using CinemaTicketBooking.Application.Abstractions;

namespace CinemaTicketBooking.Application.Features.Coupons;

/// <summary>
/// Validates a coupon code and returns its discount info (without applying it).
/// Works for both public codes and personal coupons.
/// </summary>
public class VerifyCouponQuery : IQuery<CouponValidationResultDto>
{
    public string CorrelationId { get; set; } = string.Empty;
    public string CouponCode { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
}

public class VerifyCouponHandler(
    IUnitOfWork uow)
{
    public async Task<CouponValidationResultDto> Handle(VerifyCouponQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.CouponCode))
            return new CouponValidationResultDto(false, "Vui lòng nhập mã giảm giá.", null, null, null, null, null, null);

        var code = query.CouponCode.Trim().ToUpperInvariant();
        var now = DateTimeOffset.UtcNow;

        try
        {
            // 1. Check if customer has a personal coupon with this code
            if (query.CustomerId.HasValue)
            {
                var customerCoupon = await uow.CustomerCoupons.GetByCustomerAndCodeAsync(
                    query.CustomerId.Value, code, ct);

                if (customerCoupon is not null)
                {
                    customerCoupon.ValidateAvailable(now);
                    return new CouponValidationResultDto(
                        IsValid: true,
                        ErrorMessage: null,
                        CouponCode: customerCoupon.CouponCode,
                        DiscountType: customerCoupon.DiscountType.ToString(),
                        DiscountValue: customerCoupon.DiscountValue,
                        MaxDiscountAmount: customerCoupon.MaxDiscountAmount,
                        Scope: customerCoupon.Scope.ToString(),
                        Description: $"Coupon giảm {FormatDiscount(customerCoupon.DiscountType, customerCoupon.DiscountValue, customerCoupon.MaxDiscountAmount)}");
                }
            }

            // 2. Check public template
            var template = await uow.CouponTemplates.GetByCodeAsync(code, ct);
            if (template is null)
                return new CouponValidationResultDto(false, "Mã giảm giá không hợp lệ.", null, null, null, null, null, null);

            template.ValidateAvailable(now);

            // 2b. Check per-user usage limit for public coupons
            if (template.MaxUsagePerUser > 0 && query.CustomerId.HasValue)
            {
                var userUsageCount = await uow.CustomerCoupons.GetUsageCountByCustomerAndCodeAsync(
                    query.CustomerId.Value, code, ct);
                if (userUsageCount >= template.MaxUsagePerUser)
                    return new CouponValidationResultDto(
                        false, "Bạn đã sử dụng mã giảm giá này rồi.", null, null, null, null, null, null);
            }

            return new CouponValidationResultDto(
                IsValid: true,
                ErrorMessage: null,
                CouponCode: template.Code,
                DiscountType: template.DiscountType.ToString(),
                DiscountValue: template.DiscountValue,
                MaxDiscountAmount: template.MaxDiscountAmount,
                Scope: template.Scope.ToString(),
                Description: template.Description);
        }
        catch (InvalidOperationException ex)
        {
            return new CouponValidationResultDto(false, ex.Message, null, null, null, null, null, null);
        }
    }

    private static string FormatDiscount(Domain.DiscountType type, decimal value, decimal? maxAmount)
    {
        if (type == Domain.DiscountType.Fixed)
            return $"{value:N0}đ";

        var text = $"{value}%";
        if (maxAmount.HasValue)
            text += $" (tối đa {maxAmount:N0}đ)";
        return text;
    }
}
using CinemaTicketBooking.Domain;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

/// <summary>
/// Unit tests for CouponTemplate domain entity.
/// </summary>
public class CouponTemplateTests
{
    // =============================================
    // Factory: CouponTemplate.Create
    // =============================================

    [Fact]
    public void Create_Should_SetProperties_When_ValidInput()
    {
        var template = CouponTemplate.Create(
            code: "KOL50K",
            type: CouponType.Public,
            discountType: DiscountType.Fixed,
            discountValue: 50000m,
            maxDiscountAmount: null,
            scope: DiscountScope.All,
            maxUsageCount: 100,
            maxUsagePerUser: 1,
            durationDays: 180,
            description: "Giảm 50,000đ",
            isActive: true);

        template.Code.Should().Be("KOL50K");
        template.Type.Should().Be(CouponType.Public);
        template.DiscountType.Should().Be(DiscountType.Fixed);
        template.DiscountValue.Should().Be(50000m);
        template.MaxDiscountAmount.Should().BeNull();
        template.Scope.Should().Be(DiscountScope.All);
        template.MaxUsageCount.Should().Be(100);
        template.MaxUsagePerUser.Should().Be(1);
        template.DurationDays.Should().Be(180);
        template.IsActive.Should().BeTrue();
        template.TotalUsedCount.Should().Be(0);
    }

    [Fact]
    public void Create_Should_UppercaseCode()
    {
        var template = CouponTemplate.Create("kol50k", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "");
        template.Code.Should().Be("KOL50K");
    }

    [Fact]
    public void Create_Should_Throw_When_CodeEmpty()
    {
        var act = () => CouponTemplate.Create("", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "");
        act.Should().Throw<ArgumentException>().WithParameterName("code");
    }

    [Fact]
    public void Create_Should_Throw_When_DiscountValueZero()
    {
        var act = () => CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 0m, null, DiscountScope.All, 10, 1, 30, "");
        act.Should().Throw<ArgumentException>().WithParameterName("discountValue");
    }

    [Fact]
    public void Create_Should_Throw_When_PercentageAbove100()
    {
        var act = () => CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Percentage, 101m, 50000m, DiscountScope.All, 10, 1, 30, "");
        act.Should().Throw<ArgumentException>().WithParameterName("discountValue");
    }

    [Fact]
    public void Create_Should_Throw_When_MaxDiscountAmountSetForFixedDiscount()
    {
        var act = () => CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, 5000m, DiscountScope.All, 10, 1, 30, "");
        act.Should().Throw<ArgumentException>().WithParameterName("maxDiscountAmount");
    }

    [Fact]
    public void Create_Should_Throw_When_DurationDaysZero()
    {
        var act = () => CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 0, "");
        act.Should().Throw<ArgumentException>().WithParameterName("durationDays");
    }

    // =============================================
    // ValidateAvailable
    // =============================================

    [Fact]
    public void ValidateAvailable_Should_Pass_When_ActiveAndNotExhausted()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "");
        var now = DateTimeOffset.UtcNow;

        var act = () => template.ValidateAvailable(now);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateAvailable_Should_Throw_When_Inactive()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "", isActive: false);

        var act = () => template.ValidateAvailable(DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*vô hiệu hóa*");
    }

    [Fact]
    public void ValidateAvailable_Should_Throw_When_MaxUsageReached()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 2, 1, 30, "");
        template.IncrementUsage();
        template.IncrementUsage();

        var act = () => template.ValidateAvailable(DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*hết lượt*");
    }

    // =============================================
    // IncrementUsage
    // =============================================

    [Fact]
    public void IncrementUsage_Should_Throw_When_ExceedsMax()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 1, 1, 30, "");
        template.IncrementUsage();

        var act = () => template.IncrementUsage();
        act.Should().Throw<InvalidOperationException>().WithMessage("*maximum usage count*");
    }

    [Fact]
    public void IncrementUsage_Should_AllowUnlimited_When_MaxUsageCountIsZero()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 0, 1, 30, "");

        for (var i = 0; i < 1000; i++)
        {
            template.IncrementUsage();
        }

        template.TotalUsedCount.Should().Be(1000);
    }

    // =============================================
    // ComputeExpiry
    // =============================================

    [Fact]
    public void ComputeExpiry_Should_AddDurationDays_FromGivenDate()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "");
        var from = new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero);

        var expiry = template.ComputeExpiry(from);

        expiry.Should().Be(new DateTimeOffset(2026, 6, 21, 0, 0, 0, TimeSpan.Zero));
    }

    // =============================================
    // Mutators: Activate / Deactivate
    // =============================================

    [Fact]
    public void Deactivate_Should_SetIsActiveFalse()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "");
        template.Deactivate();
        template.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_Should_SetIsActiveTrue()
    {
        var template = CouponTemplate.Create("TEST", CouponType.Public, DiscountType.Fixed, 10000m, null, DiscountScope.All, 10, 1, 30, "", isActive: false);
        template.Activate();
        template.IsActive.Should().BeTrue();
    }
}
using CinemaTicketBooking.Domain;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

/// <summary>
/// Unit tests for CustomerCoupon domain entity.
/// </summary>
public class CustomerCouponTests
{
    private static CouponTemplate PublicTemplate => CouponTemplate.Create(
        "PUBLIC10", CouponType.Public, DiscountType.Percentage, 10m, 30000m,
        DiscountScope.All, 100, 1, 180, "Public 10%");

    private static CouponTemplate PersonalTemplate => CouponTemplate.Create(
        "UPGRADE-RUBY", CouponType.Personal, DiscountType.Percentage, 20m, 100000m,
        DiscountScope.All, 0, 1, 365, "Ruby upgrade");

    private readonly DateTimeOffset _now = new(2026, 5, 22, 10, 0, 0, TimeSpan.Zero);
    private readonly Guid _customerId = Guid.CreateVersion7();

    // =============================================
    // Factory: IssueFromTemplate (Personal)
    // =============================================

    [Fact]
    public void IssueFromTemplate_Should_CreatePersonalCoupon_When_Valid()
    {
        var template = PersonalTemplate;
        var coupon = CustomerCoupon.IssueFromTemplate(_customerId, template, _now);

        coupon.CustomerId.Should().Be(_customerId);
        coupon.CouponTemplateId.Should().Be(template.Id);
        coupon.CouponCode.Should().Be("UPGRADE-RUBY");
        coupon.DiscountType.Should().Be(DiscountType.Percentage);
        coupon.DiscountValue.Should().Be(20m);
        coupon.MaxDiscountAmount.Should().Be(100000m);
        coupon.Scope.Should().Be(DiscountScope.All);
        coupon.UsageCount.Should().Be(0);
        coupon.MaxUsage.Should().Be(1);
        coupon.IssuedAt.Should().Be(_now);
        coupon.ExpiresAt.Should().Be(_now.AddDays(365));
    }

    [Fact]
    public void IssueFromTemplate_Should_Throw_When_TemplateIsPublic()
    {
        var act = () => CustomerCoupon.IssueFromTemplate(_customerId, PublicTemplate, _now);
        act.Should().Throw<ArgumentException>().WithMessage("*Personal*");
    }

    [Fact]
    public void IssueFromTemplate_Should_Throw_When_TemplateInactive()
    {
        var inactive = CouponTemplate.Create(
            "INACTIVE", CouponType.Personal, DiscountType.Fixed, 10000m, null,
            DiscountScope.All, 0, 1, 30, "", isActive: false);

        var act = () => CustomerCoupon.IssueFromTemplate(_customerId, inactive, _now);
        act.Should().Throw<InvalidOperationException>().WithMessage("*inactive*");
    }

    // =============================================
    // Factory: RedeemPublic
    // =============================================

    [Fact]
    public void RedeemPublic_Should_CreateRecord_WithUsageCountZero()
    {
        var template = PublicTemplate;
        var coupon = CustomerCoupon.RedeemPublic(_customerId, template, _now);

        coupon.CustomerId.Should().Be(_customerId);
        coupon.CouponTemplateId.Should().Be(template.Id);
        coupon.CouponCode.Should().Be("PUBLIC10");
        coupon.UsageCount.Should().Be(0);
        coupon.MaxUsage.Should().Be(1);
        coupon.IsUsed.Should().BeFalse();
        coupon.ExpiresAt.Should().Be(_now.AddDays(180));
    }

    [Fact]
    public void RedeemPublic_Should_Throw_When_TemplateIsPersonal()
    {
        var act = () => CustomerCoupon.RedeemPublic(_customerId, PersonalTemplate, _now);
        act.Should().Throw<ArgumentException>().WithMessage("*Public*");
    }

    // =============================================
    // Factory: CreatePersonal (ad-hoc)
    // =============================================

    [Fact]
    public void CreatePersonal_Should_SetProperties()
    {
        var expiresAt = _now.AddMonths(6);
        var coupon = CustomerCoupon.CreatePersonal(
            _customerId, "SILVER-THAN", DiscountType.Fixed, 20000m, null,
            DiscountScope.Concessions, expiresAt, _now);

        coupon.CustomerId.Should().Be(_customerId);
        coupon.CouponCode.Should().Be("SILVER-THAN");
        coupon.CouponTemplateId.Should().BeNull();
        coupon.UsageCount.Should().Be(0);
        coupon.MaxUsage.Should().Be(1);
        coupon.ExpiresAt.Should().Be(expiresAt);
        coupon.Scope.Should().Be(DiscountScope.Concessions);
    }

    // =============================================
    // MarkUsed
    // =============================================

    [Fact]
    public void MarkUsed_Should_IncrementUsage_And_SetUsedAt()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);

        coupon.MarkUsed(_now.AddHours(1));

        coupon.UsageCount.Should().Be(1);
        coupon.UsedAt.Should().Be(_now.AddHours(1));
        coupon.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void MarkUsed_Should_Throw_When_AlreadyUsed()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);
        coupon.MarkUsed(_now.AddHours(1));

        var act = () => coupon.MarkUsed(_now.AddHours(2));
        act.Should().Throw<InvalidOperationException>().WithMessage("*sử dụng hết*");
    }

    [Fact]
    public void MarkUsed_Should_Throw_When_Expired()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);
        var afterExpiry = _now.AddDays(181);

        var act = () => coupon.MarkUsed(afterExpiry);
        act.Should().Throw<InvalidOperationException>().WithMessage("*hết hạn*");
    }

    // =============================================
    // ValidateAvailable
    // =============================================

    [Fact]
    public void ValidateAvailable_Should_Pass_When_NotUsed_And_NotExpired()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);

        var act = () => coupon.ValidateAvailable(_now.AddDays(1));
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateAvailable_Should_Throw_When_Used()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);
        coupon.MarkUsed(_now.AddHours(1));

        var act = () => coupon.ValidateAvailable(_now.AddDays(1));
        act.Should().Throw<InvalidOperationException>().WithMessage("*sử dụng hết*");
    }

    [Fact]
    public void ValidateAvailable_Should_Throw_When_Expired()
    {
        var coupon = CustomerCoupon.RedeemPublic(_customerId, PublicTemplate, _now);
        var afterExpiry = _now.AddDays(181);

        var act = () => coupon.ValidateAvailable(afterExpiry);
        act.Should().Throw<InvalidOperationException>().WithMessage("*hết hạn*");
    }
}
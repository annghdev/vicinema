using CinemaTicketBooking.Domain;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

/// <summary>
/// Unit tests for LoyaltyTierConfiguration entity: creation and update.
/// </summary>
public class LoyaltyTierConfigurationTests
{
    // =============================================
    // Create
    // =============================================

    [Fact]
    public void Create_Should_SetAllProperties()
    {
        var config = LoyaltyTierConfiguration.Create(
            tier: LoyaltyTier.Gold,
            name: "Vàng",
            minPoints: 501,
            maxPoints: 1500,
            ticketDiscountPercent: 10m,
            concessionDiscountPercent: 20m,
            description: "Test tier",
            isActive: true);

        config.Tier.Should().Be(LoyaltyTier.Gold);
        config.Name.Should().Be("Vàng");
        config.MinPoints.Should().Be(501);
        config.MaxPoints.Should().Be(1500);
        config.TicketDiscountPercent.Should().Be(10m);
        config.ConcessionDiscountPercent.Should().Be(20m);
        config.Description.Should().Be("Test tier");
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_AllowNullMaxPoints_For_Ruby()
    {
        var config = LoyaltyTierConfiguration.Create(
            LoyaltyTier.Ruby, "Ruby", 15001, null, 30, 50);

        config.MaxPoints.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_TicketDiscountPercentOutOfRange(decimal ticketPct)
    {
        var act = () => LoyaltyTierConfiguration.Create(
            LoyaltyTier.Bronze, "Test", 0, 100, ticketPct, 10);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_Should_Throw_When_ConcessionDiscountPercentOutOfRange(decimal concessionPct)
    {
        var act = () => LoyaltyTierConfiguration.Create(
            LoyaltyTier.Bronze, "Test", 0, 100, 0, concessionPct);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_MinPointsNegative()
    {
        var act = () => LoyaltyTierConfiguration.Create(
            LoyaltyTier.Bronze, "Test", -1, 100, 0, 10);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_Should_Throw_When_MaxPointsLessThanMinPoints()
    {
        var act = () => LoyaltyTierConfiguration.Create(
            LoyaltyTier.Bronze, "Test", 500, 100, 0, 10);
        act.Should().Throw<ArgumentException>();
    }

    // =============================================
    // UpdateConfig
    // =============================================

    [Fact]
    public void UpdateConfig_Should_UpdateAllFields_And_RaiseEvent()
    {
        var config = LoyaltyTierConfiguration.Create(
            LoyaltyTier.Silver, "Bạc", 101, 500, 5, 15, "Old desc", true);
        config.ClearEvents();

        config.UpdateConfig("Bạc Premium", 200, 600, 8, 18, "Updated desc", false);

        config.Name.Should().Be("Bạc Premium");
        config.MinPoints.Should().Be(200);
        config.MaxPoints.Should().Be(600);
        config.TicketDiscountPercent.Should().Be(8);
        config.ConcessionDiscountPercent.Should().Be(18);
        config.Description.Should().Be("Updated desc");
        config.IsActive.Should().BeFalse();

        config.Events.Should().ContainSingle()
            .Which.Should().BeOfType<LoyaltyTierConfigurationUpdated>()
            .Which.Tier.Should().Be(LoyaltyTier.Silver);
    }
}

using CinemaTicketBooking.Domain;
using CinemaTicketBooking.UnitTests.Shared;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

/// <summary>
/// Unit tests for Customer loyalty-related methods: AddPoints, UpgradeTier, default values.
/// </summary>
public class CustomerLoyaltyTests
{
    private static Customer CreateRegisteredCustomer()
    {
        var customer = Customer.Create("Test User", "session-1", "0123456789", "test@example.com", true);
        customer.ClearEvents(); // Clear creation event for clean assertions
        return customer;
    }

    // =============================================
    // Default Values
    // =============================================

    [Fact]
    public void NewRegisteredCustomer_Should_HaveDefaultDongTier_And_ZeroPoints()
    {
        var customer = Customer.Create("Test", "s1", "0123456789", "a@b.com", true);

        customer.LoyaltyTier.Should().Be(LoyaltyTier.Bronze);
        customer.AccumulatedPoints.Should().Be(0);
    }

    // =============================================
    // AddPoints
    // =============================================

    [Fact]
    public void AddPoints_Should_IncreaseAccumulatedPoints_And_RaiseEvent()
    {
        var customer = CreateRegisteredCustomer();

        customer.AddPoints(50, Guid.CreateVersion7(), 50000m);

        customer.AccumulatedPoints.Should().Be(50);
        customer.Events.Should().ContainSingle()
            .Which.Should().BeOfType<LoyaltyPointsEarned>()
            .Which.PointsEarned.Should().Be(50);
    }

    [Fact]
    public void AddPoints_Should_AccumulateMultipleTimes()
    {
        var customer = CreateRegisteredCustomer();

        customer.AddPoints(100, Guid.CreateVersion7(), 100000m);
        customer.AddPoints(200, Guid.CreateVersion7(), 200000m);

        customer.AccumulatedPoints.Should().Be(300);
        customer.Events.Should().HaveCount(2);
    }

    [Fact]
    public void AddPoints_Should_Throw_When_PointsNegative()
    {
        var customer = CreateRegisteredCustomer();

        var act = () => customer.AddPoints(-1, Guid.CreateVersion7(), 0m);
        act.Should().Throw<ArgumentException>();
    }

    // =============================================
    // UpgradeTier
    // =============================================

    [Fact]
    public void UpgradeTier_Should_UpdateTier_And_RaiseEvent()
    {
        var customer = CreateRegisteredCustomer();
        // Simulate enough points to reach Bac
        customer.AddPoints(200, Guid.CreateVersion7(), 200000m);
        customer.ClearEvents();

        customer.UpgradeTier(LoyaltyTier.Silver);

        customer.LoyaltyTier.Should().Be(LoyaltyTier.Silver);
        var tierEvent = customer.Events.Should().ContainSingle()
            .Which.Should().BeOfType<LoyaltyTierUpgraded>()
            .Subject;
        tierEvent.PreviousTier.Should().Be(LoyaltyTier.Bronze);
        tierEvent.NewTier.Should().Be(LoyaltyTier.Silver);
    }

    [Fact]
    public void UpgradeTier_Should_Skip_When_TierIsLower_Or_Same()
    {
        var customer = CreateRegisteredCustomer();
        customer.AddPoints(1000, Guid.CreateVersion7(), 1000000m);
        customer.UpgradeTier(LoyaltyTier.Gold);
        customer.ClearEvents();

        // Downgrade attempt — should be ignored
        customer.UpgradeTier(LoyaltyTier.Silver);
        customer.LoyaltyTier.Should().Be(LoyaltyTier.Gold);
        customer.Events.Should().BeEmpty();

        // Same tier — should be ignored
        customer.UpgradeTier(LoyaltyTier.Gold);
        customer.LoyaltyTier.Should().Be(LoyaltyTier.Gold);
        customer.Events.Should().BeEmpty();
    }

    [Fact]
    public void UpgradeTier_Should_JumpMultipleTiers()
    {
        var customer = CreateRegisteredCustomer();
        // Simulate massive points
        customer.AddPoints(20000, Guid.CreateVersion7(), 20000000m);

        customer.UpgradeTier(LoyaltyTier.Ruby);

        customer.LoyaltyTier.Should().Be(LoyaltyTier.Ruby);
        customer.Events.Should().Contain(e => e is LoyaltyTierUpgraded);
    }
}

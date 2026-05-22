using CinemaTicketBooking.Application.Features.Loyalty;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.IntegrationTests.Shared.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.IntegrationTests.ApplicationTests.FeatureTests;

/// <summary>
/// Integration tests for loyalty queries and commands.
/// </summary>
public sealed class LoyaltyFeatureTests(PostgresContainerFixture databaseFixture)
    : ApplicationFeatureTestBase(databaseFixture)
{
    private async Task SeedLoyaltyTiersAsync()
    {
        await using var db = CreateDbContext();
        if (await db.LoyaltyTierConfigurations.AnyAsync())
            return;

        var tiers = new[]
        {
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Silver, "Bạc", 101, 500, 5, 15),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Gold, "Vàng", 501, 1500, 10, 20),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Platinum, "Bạch Kim", 1501, 5000, 15, 25),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Diamond, "Kim Cương", 5001, 15000, 20, 35),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Ruby, "Ruby", 15001, null, 30, 50)
        };
        db.LoyaltyTierConfigurations.AddRange(tiers);
        await db.SaveChangesAsync();
    }

    // =============================================
    // GetActiveLoyaltyTiersQuery
    // =============================================

    [Fact]
    public async Task GetActiveLoyaltyTiers_Should_Return_All_Tiers()
    {
        await ResetDatabaseAsync();
        await SeedLoyaltyTiersAsync();

        var result = await InvokeAsync<IReadOnlyList<LoyaltyTierDto>>(
            new GetActiveLoyaltyTiersQuery());

        result.Should().NotBeNull();
        result.Count.Should().Be(6);
        result.Should().Contain(t => t.Tier == LoyaltyTier.Bronze);
        result.Should().Contain(t => t.Tier == LoyaltyTier.Ruby);
    }

    // =============================================
    // GetCustomerLoyaltyQuery
    // =============================================

    [Fact]
    public async Task GetCustomerLoyalty_Should_Return_Loyalty_For_RegisteredCustomer()
    {
        await ResetDatabaseAsync();
        await SeedLoyaltyTiersAsync();

        await using var db = CreateDbContext();
        var customer = Customer.Create("Loyalty Test", "loyalty-session", "0123456789", "loyalty@test.com", true);
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        var result = await InvokeAsync<CustomerLoyaltyDto?>(
            new GetCustomerLoyaltyQuery { CustomerId = customer.Id });

        result.Should().NotBeNull();
        result!.CurrentTier.Should().Be(LoyaltyTier.Bronze);
        result.AccumulatedPoints.Should().Be(0);
        result.TicketDiscountPercent.Should().Be(0);
        result.ConcessionDiscountPercent.Should().Be(10);
        result.NextTier.Should().Be(LoyaltyTier.Silver);
        result.PointsToNextTier.Should().Be(101);
    }

    [Fact]
    public async Task GetCustomerLoyalty_Should_Return_Null_For_Guest()
    {
        await ResetDatabaseAsync();
        await SeedLoyaltyTiersAsync();

        await using var db = CreateDbContext();
        var guest = Customer.Create("Guest", "guest-session", "0123456789", "guest@test.com", false);
        db.Customers.Add(guest);
        await db.SaveChangesAsync();

        var result = await InvokeAsync<CustomerLoyaltyDto?>(
            new GetCustomerLoyaltyQuery { CustomerId = guest.Id });

        result.Should().BeNull();
    }

    // =============================================
    // UpdateLoyaltyTierConfigurationCommand
    // =============================================

    [Fact]
    public async Task UpdateLoyaltyTier_Should_UpdateConfiguration()
    {
        await ResetDatabaseAsync();
        await SeedLoyaltyTiersAsync();

        // 1. Load tier config ID
        Guid dongTierId;
        await using (var db = CreateDbContext())
        {
            var dongTier = await db.LoyaltyTierConfigurations
                .FirstAsync(t => t.Tier == LoyaltyTier.Bronze);
            dongTierId = dongTier.Id;
        }

        // 2. Update via Wolverine
        await InvokeAsync(new UpdateLoyaltyTierConfigurationCommand
        {
            Id = dongTierId,
            Name = "Đồng Updated",
            MinPoints = 0,
            MaxPoints = 150,
            TicketDiscountPercent = 2,
            ConcessionDiscountPercent = 12,
            Description = "Updated tier",
            IsActive = true
        });

        // 3. Verify via fresh context
        await using var verifyDb = CreateDbContext();
        var updated = await verifyDb.LoyaltyTierConfigurations
            .AsNoTracking()
            .FirstAsync(t => t.Id == dongTierId);
        updated.Name.Should().Be("Đồng Updated");
        updated.MaxPoints.Should().Be(150);
        updated.TicketDiscountPercent.Should().Be(2);
        updated.ConcessionDiscountPercent.Should().Be(12);
    }
}

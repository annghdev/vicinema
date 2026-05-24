using CinemaTicketBooking.Application.Features;
using CinemaTicketBooking.Application.Features.Bookings.Commands;
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Services;
using CinemaTicketBooking.IntegrationTests.Shared.DataSeeders;
using CinemaTicketBooking.IntegrationTests.Shared.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.IntegrationTests.ApplicationTests.FeatureTests;

/// <summary>
/// Integration tests verifying coupon discount application during booking creation
/// for different loyalty tier customers.
/// </summary>
public sealed class CouponBookingFeatureTests(PostgresContainerFixture databaseFixture)
    : ApplicationFeatureTestBase(databaseFixture)
{
    // =============================================
    // Shared test data IDs for coupon booking tests
    // =============================================

    private const string SessionId = "coupon-it-session-1";
    private const string BronzeCustomerEmail = "bronze.coupon@test.com";
    private const string SilverCustomerEmail = "silver.coupon@test.com";
    private const string CouponCode = "KOL50K";
    private const string PercentageCouponCode = "VICINEMA10";

    // =============================================
    // Test 1: Bronze tier customer + Fixed coupon (KOL50K)
    //
    // Bronze tier: 0% ticket discount, 10% concession discount
    // KOL50K: Fixed 50,000đ on All scope
    //
    // OriginAmount = 2 tickets (100k each) + 1 concession (80k) = 280,000đ
    // Loyalty discount = 200,000 * 0% + 80,000 * 10% = 8,000đ
    // Coupon discount = min(50,000, 280,000) = 50,000đ
    // FinalAmount = 280,000 - 8,000 - 50,000 = 222,000đ
    // =============================================

    [Fact]
    public async Task CreateBooking_WithBronzeCustomer_AndKOL50KCoupon_ShouldReduceFinalAmount()
    {
        await ResetDatabaseAsync();

        // Arrange: seed test data including registered Bronze customer + coupon + loyalty tiers
        var seed = await SeedCouponCheckoutGraphAsync(LoyaltyTier.Bronze, BronzeCustomerEmail);
        await SeedLoyaltyTiersWithBronzeAndSilverAsync();
        await SeedKol50kCouponTemplateAsync();
        await SeedVicinenma10CouponTemplateAsync();
        ConfigureFakeUserContext(seed.CustomerId, "BookingsCreate");

        // Act: create booking with coupon code
        var response = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = SessionId,
            CustomerName = "Bronze Coupon Test",
            CustomerPhoneNumber = "0912345678",
            CustomerEmail = BronzeCustomerEmail,
            SelectedTicketIds =
            [
                seed.TicketsBySeatCode["A1"],
                seed.TicketsBySeatCode["A2"]
            ],
            Concessions = [new CheckoutConcessionSelection(seed.ConcessionId, 1)],
            CouponCode = CouponCode,
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-bronze-kol50k"
        });

        // Assert: verify coupon discount applied correctly
        response.OriginAmount.Should().Be(280_000m);
        response.FinalAmount.Should().Be(222_000m); // 280,000 - 8,000 (loyalty) - 50,000 (coupon)

        // Verify persisted booking has correct values
        await using var db = CreateDbContext();
        var booking = await db.Bookings
            .AsNoTracking()
            .SingleAsync(x => x.Id == response.BookingId);

        booking.OriginAmount.Should().Be(280_000m);
        booking.FinalAmount.Should().Be(222_000m);
        booking.CouponCode.Should().Be(CouponCode);
        booking.CouponDiscountAmount.Should().Be(50_000m);

        // Verify coupon usage was recorded: 1 customer_coupon record + template usage incremented
        var usageRecords = await db.CustomerCoupons
            .Where(c => c.CouponCode == CouponCode)
            .ToListAsync();
        usageRecords.Should().ContainSingle(r => r.CustomerId == seed.CustomerId);

        var template = await db.CouponTemplates.SingleAsync(t => t.Code == CouponCode);
        template.TotalUsedCount.Should().Be(1);
    }

    // =============================================
    // Test 2: Silver tier customer + Percentage coupon (VICINEMA10)
    //
    // Silver tier: 5% ticket discount, 15% concession discount
    // VICINEMA10: 10% on All scope, max 30,000đ
    //
    // OriginAmount = 2 tickets (100k each) + 1 concession (80k) = 280,000đ
    // Coupon discount = min(280,000 * 10%, 30,000) = 28,000đ
    // Loyalty discount = 200,000 * 5% + 80,000 * 15% = 10,000 + 12,000 = 22,000đ
    // FinalAmount = 280,000 - 22,000 - 28,000 = 230,000đ
    // =============================================

    [Fact]
    public async Task CreateBooking_WithSilverCustomer_AndVICINEMA10Coupon_ShouldReduceFinalAmount()
    {
        await ResetDatabaseAsync();

        // Arrange: seed test data including registered Silver customer + coupon + loyalty tiers
        var seed = await SeedCouponCheckoutGraphAsync(LoyaltyTier.Silver, SilverCustomerEmail);
        await SeedLoyaltyTiersWithBronzeAndSilverAsync();
        await SeedKol50kCouponTemplateAsync();
        await SeedVicinenma10CouponTemplateAsync();
        ConfigureFakeUserContext(seed.CustomerId, "BookingsCreate");

        // Act: create booking with percentage coupon
        var response = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = SessionId,
            CustomerName = "Silver Coupon Test",
            CustomerPhoneNumber = "0987654321",
            CustomerEmail = SilverCustomerEmail,
            SelectedTicketIds =
            [
                seed.TicketsBySeatCode["A1"],
                seed.TicketsBySeatCode["A2"]
            ],
            Concessions = [new CheckoutConcessionSelection(seed.ConcessionId, 1)],
            CouponCode = PercentageCouponCode,
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-silver-vicinenma10"
        });

        // Assert: verify both loyalty and coupon discounts applied
        response.OriginAmount.Should().Be(280_000m);
        response.FinalAmount.Should().Be(230_000m); // 280,000 - 22,000 (loyalty) - 28,000 (coupon)

        // Verify persisted booking
        await using var db = CreateDbContext();
        var booking = await db.Bookings
            .AsNoTracking()
            .SingleAsync(x => x.Id == response.BookingId);

        booking.OriginAmount.Should().Be(280_000m);
        booking.FinalAmount.Should().Be(230_000m);
        booking.CouponCode.Should().Be(PercentageCouponCode);
        booking.CouponDiscountAmount.Should().Be(28_000m);

        // Verify coupon template usage incremented
        var template = await db.CouponTemplates.SingleAsync(t => t.Code == PercentageCouponCode);
        template.TotalUsedCount.Should().Be(1);
    }

    // =============================================
    // Test 3: Create booking WITHOUT coupon — verify coupon fields are empty
    // =============================================

    [Fact]
    public async Task CreateBooking_WithoutCoupon_ShouldHaveZeroCouponFields()
    {
        await ResetDatabaseAsync();
        var seed = await SeedCouponCheckoutGraphAsync(LoyaltyTier.Bronze, "no.coupon@test.com");
        await SeedLoyaltyTiersWithBronzeAndSilverAsync();
        ConfigureFakeUserContext(seed.CustomerId, "BookingsCreate");

        var response = await InvokeAsync<CreateBookingResponse>(new CreateBookingCommand
        {
            ShowTimeId = seed.ShowTimeId,
            CustomerSessionId = SessionId,
            CustomerName = "No Coupon",
            CustomerPhoneNumber = "0900000000",
            CustomerEmail = "no.coupon@test.com",
            SelectedTicketIds = [seed.TicketsBySeatCode["A1"]],
            Concessions = [],
            PaymentMethod = "None",
            ReturnUrl = "https://localhost/checkout/return",
            IpAddress = "127.0.0.1",
            CorrelationId = "it-no-coupon"
        });

        await using var db = CreateDbContext();
        var booking = await db.Bookings
            .AsNoTracking()
            .SingleAsync(x => x.Id == response.BookingId);

        booking.CouponCode.Should().BeNull();
        booking.CouponDiscountAmount.Should().Be(0m);
        // Without coupon: FinalAmount = OriginAmount - loyalty discount
        // Bronze 0% ticket, 0 loyalty discount for single ticket only
        booking.FinalAmount.Should().Be(booking.OriginAmount);
    }

    // =============================================
    // Seed helpers
    // =============================================

    /// <summary>
    /// Seeds cinema, movie, screen, showtime, tickets, a registered customer with the given tier,
    /// and a concession. Tickets are pre-locked for the shared session ID.
    /// </summary>
    private async Task<CouponCheckoutSeed> SeedCouponCheckoutGraphAsync(LoyaltyTier tier, string email)
    {
        await using var db = CreateDbContext();

        var cinema = IntegrationEntityBuilder.Cinema("Coupon Test Cinema");
        var movie = IntegrationEntityBuilder.Movie("Coupon Test Movie", MovieStatus.NowShowing);
        var screen = IntegrationEntityBuilder.Screen(cinema.Id, "CPN-SCR-1", "[[1,1,1,1,1]]");
        var showTime = IntegrationEntityBuilder.ShowTime(movie.Id, screen.Id);

        var customer = new Customer
        {
            Id = Guid.CreateVersion7(),
            Name = $"Coupon Test {tier}",
            SessionId = SessionId,
            PhoneNumber = "0900000000",
            Email = email,
            IsRegistered = true,
            LoyaltyTier = tier,
            AccumulatedPoints = tier == LoyaltyTier.Silver ? 200 : 50
        };

        var seatCodes = new[] { "A1", "A2", "A3" };
        var tickets = seatCodes.Select(seatCode => new Ticket
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = showTime.Id,
            SeatId = screen.Seats.FirstOrDefault(x => x.Code == seatCode)?.Id,
            SeatCode = seatCode,
            Status = TicketStatus.Locking,
            LockingBy = SessionId,
            LockExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Code = $"20260415-CPN-{seatCode}",
            Description = $"{seatCode} - Regular",
            Price = 100_000m
        }).ToList();

        var concession = IntegrationEntityBuilder.Concession("Test Combo", 80_000m);

        db.Cinemas.Add(cinema);
        db.Movies.Add(movie);
        db.Screens.Add(screen);
        db.ShowTimes.Add(showTime);
        db.Customers.Add(customer);
        db.Tickets.AddRange(tickets);
        db.Concessions.Add(concession);
        await db.SaveChangesAsync();

        return new CouponCheckoutSeed(
            showTime.Id,
            customer.Id,
            concession.Id,
            tickets.ToDictionary(t => t.SeatCode, t => t.Id));
    }

    private async Task SeedLoyaltyTiersWithBronzeAndSilverAsync()
    {
        await using var db = CreateDbContext();
        if (await db.LoyaltyTierConfigurations.AnyAsync())
            return;

        var tiers = new[]
        {
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Silver, "Bạc", 101, 500, 5, 15),
        };
        db.LoyaltyTierConfigurations.AddRange(tiers);
        await db.SaveChangesAsync();
    }

    private async Task SeedKol50kCouponTemplateAsync()
    {
        await using var db = CreateDbContext();
        if (await db.CouponTemplates.AnyAsync(t => t.Code == CouponCode))
            return;

        var template = CouponTemplate.Create(
            code: CouponCode,
            type: CouponType.Public,
            discountType: DiscountType.Fixed,
            discountValue: 50_000m,
            maxDiscountAmount: null,
            scope: DiscountScope.All,
            maxUsageCount: 100,
            maxUsagePerUser: 1,
            durationDays: 180,
            description: "Mã KOL — Giảm 50,000đ toàn bộ hóa đơn",
            isActive: true);
        db.CouponTemplates.Add(template);
        await db.SaveChangesAsync();
    }

    private async Task SeedVicinenma10CouponTemplateAsync()
    {
        await using var db = CreateDbContext();
        if (await db.CouponTemplates.AnyAsync(t => t.Code == PercentageCouponCode))
            return;

        var template = CouponTemplate.Create(
            code: PercentageCouponCode,
            type: CouponType.Public,
            discountType: DiscountType.Percentage,
            discountValue: 10m,
            maxDiscountAmount: 30_000m,
            scope: DiscountScope.All,
            maxUsageCount: 200,
            maxUsagePerUser: 1,
            durationDays: 180,
            description: "Giảm 10% toàn bộ hóa đơn (tối đa 30,000đ)",
            isActive: true);
        db.CouponTemplates.Add(template);
        await db.SaveChangesAsync();
    }

    private void ConfigureFakeUserContext(Guid? customerId, params string[] permissions)
    {
        FakeUserContext.IsAuthenticated = true;
        FakeUserContext.CustomerId = customerId;
        FakeUserContext.Permissions = new HashSet<string>(permissions, StringComparer.Ordinal);
    }

    /// <summary>
    /// Record holding seed data for coupon booking tests.
    /// </summary>
    private sealed record CouponCheckoutSeed(
        Guid ShowTimeId,
        Guid CustomerId,
        Guid ConcessionId,
        Dictionary<string, Guid> TicketsBySeatCode);
}
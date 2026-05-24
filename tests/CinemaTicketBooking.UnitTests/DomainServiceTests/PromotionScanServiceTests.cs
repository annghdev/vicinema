using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Enums;
using CinemaTicketBooking.Domain.Services;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.DomainServiceTests;

/// <summary>
/// Unit tests for PromotionScanService and condition evaluation (TC-006 to TC-015).
/// </summary>
public class PromotionScanServiceTests
{
    private readonly PromotionScanService _sut = new();

    private static PromotionProgram CreatePromo(
        PromotionDiscountType discountType,
        DiscountForm? discountForm = null,
        decimal discountValue = 0m,
        decimal? maxDiscountAmount = null,
        decimal? maxDiscountPercentage = null,
        int? maxUsagePerCustomer = null,
        params PromotionCondition[] conditions)
    {
        var promo = PromotionProgram.Create(
            "Test Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(30),
            discountType, discountForm, discountValue,
            maxDiscountAmount, maxDiscountPercentage, maxUsagePerCustomer);

        foreach (var c in conditions)
            promo.AddCondition(c);

        return promo;
    }

    private static PromotionScanContext DefaultContext(decimal originAmount = 200_000m, int ticketCount = 1,
        decimal ticketAmount = 150_000m, decimal concessionAmount = 50_000m)
    {
        return new PromotionScanContext
        {
            BookingOriginAmount = originAmount,
            TicketAmount = ticketAmount,
            ConcessionAmount = concessionAmount,
            TicketCount = ticketCount,
            SeatTypes = ["Standard"],
            ShowtimeFormat = "2D"
        };
    }

    // =============================================
    // TC-006: Age condition
    // =============================================

    [Fact]
    public async Task AgeCondition_Should_Match_WhenWithinRange()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.Age, "18", "35"));
        var context = DefaultContext() with { CustomerAge = 25, CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task AgeCondition_Should_Fail_WhenBelowMinAge()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.Age, "18", "35"));
        var context = DefaultContext() with { CustomerAge = 14, CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    [Fact]
    public async Task AgeCondition_Should_Fail_WhenAboveMaxAge()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.Age, "18", "35"));
        var context = DefaultContext() with { CustomerAge = 40, CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    [Fact]
    public async Task AgeCondition_Should_Fail_WhenCustomerAgeNull()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.Age, "18", "35"));
        var context = DefaultContext() with { CustomerAge = null };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-007: BirthdayMonth condition
    // =============================================

    [Fact]
    public async Task BirthdayMonthCondition_Should_Match_WhenCorrectMonth()
    {
        var promo = CreatePromo(PromotionDiscountType.TicketOnly, DiscountForm.Fixed, 30000m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.BirthdayMonth, "5", null));
        var context = DefaultContext() with { CustomerBirthMonth = 5, CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task BirthdayMonthCondition_Should_Fail_WhenWrongMonth()
    {
        var promo = CreatePromo(PromotionDiscountType.TicketOnly, DiscountForm.Fixed, 30000m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.BirthdayMonth, "5", null));
        var context = DefaultContext() with { CustomerBirthMonth = 6, CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    [Fact]
    public async Task BirthdayMonthCondition_Should_Fail_WhenCustomerBirthMonthNull()
    {
        var promo = CreatePromo(PromotionDiscountType.TicketOnly, DiscountForm.Fixed, 30000m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.BirthdayMonth, "5", null));
        var context = DefaultContext() with { CustomerBirthMonth = null };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-008: MinOrderAmount condition
    // =============================================

    [Fact]
    public async Task MinOrderAmountCondition_Should_Match_WhenAboveThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null));
        var context = DefaultContext(originAmount: 250_000m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task MinOrderAmountCondition_Should_Match_WhenExactThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null));
        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task MinOrderAmountCondition_Should_Fail_WhenBelowThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null));
        var context = DefaultContext(originAmount: 199_999m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-009: MinTickets condition
    // =============================================

    [Fact]
    public async Task MinTicketsCondition_Should_Match_WhenAboveThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.FreeConcession,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "3", null));
        var context = DefaultContext(ticketCount: 4);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task MinTicketsCondition_Should_Match_WhenExactThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.FreeConcession,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "3", null));
        var context = DefaultContext(ticketCount: 3);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task MinTicketsCondition_Should_Fail_WhenBelowThreshold()
    {
        var promo = CreatePromo(PromotionDiscountType.FreeConcession,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "3", null));
        var context = DefaultContext(ticketCount: 2);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-010: CustomerTier condition
    // =============================================

    [Fact]
    public async Task CustomerTierCondition_Should_Match_WhenExactTier()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 10m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.CustomerTier, "Gold", null));
        var context = DefaultContext() with { CustomerTier = "Gold", CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task CustomerTierCondition_Should_Fail_WhenWrongTier()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 10m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.CustomerTier, "Gold", null));
        var context = DefaultContext() with { CustomerTier = "Bronze", CustomerId = Guid.NewGuid() };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    [Fact]
    public async Task CustomerTierCondition_Should_Fail_WhenCustomerTierNull()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 10m,
            conditions: PromotionCondition.Create(Guid.NewGuid(), ConditionType.CustomerTier, "Gold", null));
        var context = DefaultContext() with { CustomerTier = null };

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-011: AND logic
    // =============================================

    [Fact]
    public async Task AndLogic_Should_Match_WhenAllConditionsMet()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: [
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "2", null),
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null)
            ]);
        var context = DefaultContext(ticketCount: 3, originAmount: 250_000m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    [Fact]
    public async Task AndLogic_Should_Fail_WhenOneConditionFails()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: [
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "2", null),
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null)
            ]);
        var context = DefaultContext(ticketCount: 3, originAmount: 150_000m); // order amount fails

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    [Fact]
    public async Task AndLogic_Should_Fail_WhenOtherConditionFails()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m,
            conditions: [
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinTickets, "2", null),
                PromotionCondition.Create(Guid.NewGuid(), ConditionType.MinOrderAmount, "200000", null)
            ]);
        var context = DefaultContext(ticketCount: 1, originAmount: 250_000m); // tickets fail

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().BeEmpty();
    }

    // =============================================
    // TC-012: No conditions (global promotion)
    // =============================================

    [Fact]
    public async Task GlobalPromotion_WithoutConditions_Should_AlwaysMatch()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 15m); // no conditions = global
        var context = DefaultContext();

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions.Should().HaveCount(1);
    }

    // =============================================
    // TC-013: Stacking — Same type = highest wins
    // =============================================

    [Fact]
    public async Task Stacking_SameType_Should_SelectHighestDiscount()
    {
        var promoA = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 20000m); // 20k fixed
        var promoB = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 50000m); // 50k fixed
        var promoC = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 30000m); // 30k fixed

        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promoA, promoB, promoC], context);

        result.AppliedPromotions.Should().HaveCount(1); // only one OrderTotal
        result.AppliedPromotions[0].DiscountAmount.Should().Be(50000m);
        result.AppliedPromotions[0].PromotionName.Should().Be("Test Promo"); // promoB (50k winner)
    }

    // =============================================
    // TC-014: Stacking — Different types = cumulative
    // =============================================

    [Fact]
    public async Task Stacking_DifferentTypes_Should_BeCumulative_ButNotNegative()
    {
        var promoOrder = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 30000m);
        var promoTicket = CreatePromo(PromotionDiscountType.TicketOnly, DiscountForm.Fixed, 20000m);
        var promoConcession = CreatePromo(PromotionDiscountType.Concession, DiscountForm.Fixed, 15000m);

        var context = DefaultContext(originAmount: 50_000m, ticketAmount: 30_000m, concessionAmount: 20_000m);

        var result = await _sut.ScanAsync([promoOrder, promoTicket, promoConcession], context);

        result.AppliedPromotions.Should().HaveCount(3);
        result.TotalDiscount.Should().Be(50_000m); // capped at originAmount (50k)
        // 30k + 20k + 15k = 65k, but total is capped to 50k
    }

    // =============================================
    // TC-015: FreeConcession
    // =============================================

    [Fact]
    public async Task FreeConcession_Should_ReturnFreeItems()
    {
        var promo = CreatePromo(PromotionDiscountType.FreeConcession);
        var concessionId = Guid.CreateVersion7();
        promo.AddFreeConcessionItem(PromotionFreeConcessionItem.Create(promo.Id, concessionId, 1));

        var context = DefaultContext();

        var result = await _sut.ScanAsync([promo], context);

        result.FreeItems.Should().HaveCount(1);
        result.FreeItems[0].ConcessionId.Should().Be(concessionId);
        result.FreeItems[0].Quantity.Should().Be(1);
        result.AppliedPromotions.Should().HaveCount(1);
        result.AppliedPromotions[0].DiscountAmount.Should().Be(0); // free items = no monetary discount
    }

    [Fact]
    public async Task FreeConcession_Multiple_Should_MergeByConcessionId()
    {
        var concessionId = Guid.CreateVersion7();
        var promoA = CreatePromo(PromotionDiscountType.FreeConcession);
        promoA.AddFreeConcessionItem(PromotionFreeConcessionItem.Create(promoA.Id, concessionId, 1));

        var promoB = CreatePromo(PromotionDiscountType.FreeConcession);
        promoB.AddFreeConcessionItem(PromotionFreeConcessionItem.Create(promoB.Id, concessionId, 2));

        var context = DefaultContext();

        var result = await _sut.ScanAsync([promoA, promoB], context);

        result.FreeItems.Should().HaveCount(1);
        result.FreeItems[0].ConcessionId.Should().Be(concessionId);
        result.FreeItems[0].Quantity.Should().Be(2); // higher quantity wins
    }

    // =============================================
    // Discount calculation tests (TC-013 support)
    // =============================================

    [Fact]
    public async Task PercentageDiscount_Should_CalculateCorrectly()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 20m);
        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions[0].DiscountAmount.Should().Be(40_000m); // 20% of 200k
    }

    [Fact]
    public async Task PercentageDiscount_WithMaxCap_Should_RespectCap()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Percentage, 50m, maxDiscountAmount: 30000m);
        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        // 50% of 200k = 100k, but capped at 30k
        result.AppliedPromotions[0].DiscountAmount.Should().Be(30_000m);
    }

    [Fact]
    public async Task FixedDiscount_Should_CalculateCorrectly()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 50000m);
        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        result.AppliedPromotions[0].DiscountAmount.Should().Be(50_000m);
    }

    [Fact]
    public async Task FixedDiscount_WithMaxPercentage_Should_RespectCap()
    {
        var promo = CreatePromo(PromotionDiscountType.OrderTotal, DiscountForm.Fixed, 50000m, maxDiscountPercentage: 20m);
        var context = DefaultContext(originAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        // 50k fixed, max 20% of 200k = 40k → cap at 40k
        result.AppliedPromotions[0].DiscountAmount.Should().Be(40_000m);
    }

    [Fact]
    public async Task TicketOnlyDiscount_Should_UseTicketAmount()
    {
        var promo = CreatePromo(PromotionDiscountType.TicketOnly, DiscountForm.Percentage, 50m);
        var context = DefaultContext(originAmount: 300_000m, ticketAmount: 100_000m, concessionAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        // 50% of ticketAmount (100k) = 50k
        result.AppliedPromotions[0].DiscountAmount.Should().Be(50_000m);
    }

    [Fact]
    public async Task ConcessionDiscount_Should_UseConcessionAmount()
    {
        var promo = CreatePromo(PromotionDiscountType.Concession, DiscountForm.Percentage, 50m);
        var context = DefaultContext(originAmount: 300_000m, ticketAmount: 100_000m, concessionAmount: 200_000m);

        var result = await _sut.ScanAsync([promo], context);

        // 50% of concessionAmount (200k) = 100k
        result.AppliedPromotions[0].DiscountAmount.Should().Be(100_000m);
    }
}

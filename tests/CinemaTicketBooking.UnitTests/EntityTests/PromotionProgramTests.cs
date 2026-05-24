using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Enums;
using CinemaTicketBooking.Domain.Events;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

/// <summary>
/// Unit tests for PromotionProgram domain entity (TC-001 to TC-005).
/// </summary>
public class PromotionProgramTests
{
    // =============================================
    // TC-001: Factory — Valid Creation
    // =============================================

    [Fact]
    public void Create_Should_SetAllProperties_And_RaisePromotionCreated()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddDays(30);

        var promo = PromotionProgram.Create(
            "Summer Sale",
            "Mô tả khuyến mãi",
            "/images/promo.jpg",
            start,
            end,
            PromotionDiscountType.OrderTotal,
            DiscountForm.Percentage,
            20m,
            50000m,
            null,
            1);

        promo.Name.Should().Be("Summer Sale");
        promo.Description.Should().Be("Mô tả khuyến mãi");
        promo.PosterImage.Should().Be("/images/promo.jpg");
        promo.StartDate.Should().Be(start);
        promo.EndDate.Should().Be(end);
        promo.DiscountType.Should().Be(PromotionDiscountType.OrderTotal);
        promo.DiscountForm.Should().Be(DiscountForm.Percentage);
        promo.DiscountValue.Should().Be(20m);
        promo.MaxDiscountAmount.Should().Be(50000m);
        promo.MaxDiscountPercentage.Should().BeNull();
        promo.MaxUsagePerCustomer.Should().Be(1);
        promo.IsActive.Should().BeTrue();
        promo.DeletedAt.Should().BeNull();
        promo.Id.Should().NotBeEmpty();

        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionCreated>()
            .Which.PromotionProgramId.Should().Be(promo.Id);
    }

    [Fact]
    public void Create_Should_SetDefaults_ForOptionalFields()
    {
        var promo = PromotionProgram.Create(
            "Basic Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.TicketOnly, DiscountForm.Fixed,
            30000m, null, null, null);

        promo.Description.Should().BeNull();
        promo.PosterImage.Should().BeNull();
        promo.MaxDiscountAmount.Should().BeNull();
        promo.MaxDiscountPercentage.Should().BeNull();
        promo.MaxUsagePerCustomer.Should().BeNull(); // unlimited
        promo.IsActive.Should().BeTrue();
    }

    // =============================================
    // TC-002: Factory — Invalid Input Validation
    // =============================================

    [Fact]
    public void Create_Should_Throw_When_NameEmpty()
    {
        var act = () => PromotionProgram.Create(
            "", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            20m, null, null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Create_Should_Throw_When_EndDateNotAfterStartDate()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start; // equal = invalid

        var act = () => PromotionProgram.Create(
            "Test", null, null, start, end,
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            20m, null, null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("startDate");
    }

    [Fact]
    public void Create_Should_Throw_When_NegativeDiscountValue()
    {
        var act = () => PromotionProgram.Create(
            "Test", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Fixed,
            -1m, null, null, null);

        act.Should().Throw<ArgumentException>().WithParameterName("discountValue");
    }

    // Note: DiscountValue=0 for non-FreeConcession and Percentage>100 validations
    // are handled at the Application layer via FluentValidation, not Domain layer.
    [Fact]
    public void Create_Should_Allow_DiscountValueZero_ForNonFreeConcession_DomainDoesNotValidate()
    {
        var promo = PromotionProgram.Create(
            "Test", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Fixed,
            0m, null, null, null);

        promo.DiscountValue.Should().Be(0m);
        promo.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Allow_PercentageOver100_DomainDoesNotValidate()
    {
        var promo = PromotionProgram.Create(
            "Test", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            101m, null, null, null);

        promo.DiscountValue.Should().Be(101m);
        promo.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_Should_Allow_DiscountValueZero_ForFreeConcession()
    {
        var promo = PromotionProgram.Create(
            "Free Item", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.FreeConcession, null,
            0m, null, null, null);

        promo.DiscountValue.Should().Be(0m);
        promo.IsActive.Should().BeTrue();
    }

    // =============================================
    // TC-003: UpdateBasicInfo
    // =============================================

    [Fact]
    public void UpdateBasicInfo_Should_UpdateAllFields_And_RaisePromotionUpdated()
    {
        var promo = PromotionProgram.Create(
            "Original", "Original Desc", null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(10),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        // Clear events from creation
        promo.ClearEvents();

        var newStart = DateTimeOffset.UtcNow.AddDays(1);
        var newEnd = newStart.AddDays(30);

        promo.UpdateBasicInfo(
            "Updated", "Updated Desc", "/new.jpg",
            newStart, newEnd,
            PromotionDiscountType.TicketOnly, DiscountForm.Fixed,
            50000m, null, 50m, 1);

        promo.Name.Should().Be("Updated");
        promo.Description.Should().Be("Updated Desc");
        promo.PosterImage.Should().Be("/new.jpg");
        promo.StartDate.Should().Be(newStart);
        promo.EndDate.Should().Be(newEnd);
        promo.DiscountType.Should().Be(PromotionDiscountType.TicketOnly);
        promo.DiscountForm.Should().Be(DiscountForm.Fixed);
        promo.DiscountValue.Should().Be(50000m);
        promo.MaxDiscountPercentage.Should().Be(50m);
        promo.MaxUsagePerCustomer.Should().Be(1);

        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionUpdated>();
    }

    // =============================================
    // TC-004: SoftDelete
    // =============================================

    [Fact]
    public void SoftDelete_Should_SetDeletedAt_And_RaisePromotionDeleted()
    {
        var promo = PromotionProgram.Create(
            "To Delete", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        promo.ClearEvents();

        promo.SoftDelete();

        promo.DeletedAt.Should().NotBeNull();
        promo.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionDeleted>()
            .Which.PromotionProgramId.Should().Be(promo.Id);
    }

    // =============================================
    // TC-005: Toggle (Activate / Deactivate)
    // =============================================

    [Fact]
    public void Toggle_Should_Deactivate_WhenActive()
    {
        var promo = PromotionProgram.Create(
            "Active Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        promo.ClearEvents();

        promo.Toggle();

        promo.IsActive.Should().BeFalse();

        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionDeactivated>();
    }

    [Fact]
    public void Toggle_Should_Activate_WhenInactive()
    {
        var promo = PromotionProgram.Create(
            "Inactive Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        // Manually set inactive
        promo.GetType().GetProperty("IsActive")?.SetValue(promo, false);
        promo.ClearEvents();

        promo.Toggle();

        promo.IsActive.Should().BeTrue();

        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionActivated>();
    }

    [Fact]
    public void Toggle_Should_BeIdempotent_WhenAlreadyInactive()
    {
        var promo = PromotionProgram.Create(
            "Inactive Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        promo.GetType().GetProperty("IsActive")?.SetValue(promo, false);
        promo.ClearEvents();

        promo.Toggle(); // inactive → active
        promo.ClearEvents();
        promo.Toggle(); // active → inactive
        promo.ClearEvents();
        promo.Toggle(); // inactive → active
        promo.ClearEvents();
        promo.Toggle(); // active → inactive

        promo.IsActive.Should().BeFalse();
        var events = promo.Events;
        events.Should().HaveCount(1);
        events.First().Should().BeOfType<PromotionDeactivated>();
    }

    // =============================================
    // Conditions management
    // =============================================

    [Fact]
    public void AddCondition_Should_AddToCollection()
    {
        var promo = PromotionProgram.Create(
            "With Condition", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        var condition = PromotionCondition.Create(promo.Id, ConditionType.MinTickets, "3", null);
        promo.AddCondition(condition);

        promo.Conditions.Should().HaveCount(1);
        promo.Conditions[0].ConditionType.Should().Be(ConditionType.MinTickets);
        promo.Conditions[0].Value.Should().Be("3");
    }

    [Fact]
    public void RemoveCondition_Should_RemoveFromCollection()
    {
        var promo = PromotionProgram.Create(
            "With Condition", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.OrderTotal, DiscountForm.Percentage,
            10m, null, null, null);

        var condition = PromotionCondition.Create(promo.Id, ConditionType.MinTickets, "3", null);
        promo.AddCondition(condition);
        promo.Conditions.Should().HaveCount(1);

        promo.RemoveCondition(condition.Id);
        promo.Conditions.Should().BeEmpty();
    }

    [Fact]
    public void AddFreeConcessionItem_Should_AddToCollection()
    {
        var promo = PromotionProgram.Create(
            "Free Item Promo", null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1),
            PromotionDiscountType.FreeConcession, null,
            0m, null, null, null);

        var item = PromotionFreeConcessionItem.Create(promo.Id, Guid.CreateVersion7(), 1);
        promo.AddFreeConcessionItem(item);

        promo.FreeConcessionItems.Should().NotBeNull();
        promo.FreeConcessionItems.Should().HaveCount(1);
    }
}

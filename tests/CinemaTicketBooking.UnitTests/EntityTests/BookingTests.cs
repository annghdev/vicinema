using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Enums;
using CinemaTicketBooking.Domain.Events;
using CinemaTicketBooking.UnitTests.Shared;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.EntityTests;

public class BookingTests
{
    [Fact]
    public void AddTicket_Should_IncreaseOriginAmount_When_LockMatchesSessionId()
    {
        var showTimeId = Guid.CreateVersion7();
        var customer = DomainTestBuilders.GuestCustomer("sess-1");
        var booking = DomainTestBuilders.PendingBooking(showTimeId, customer);
        var ticket = DomainTestBuilders.LockingTicket(showTimeId, 50_000m, "sess-1");

        booking.AddTicket(ticket);

        booking.Tickets.Should().HaveCount(1);
        booking.OriginAmount.Should().Be(50_000m);
    }

    [Fact]
    public void AddTicket_Should_IncreaseOriginAmount_When_LockMatchesCustomerIdString()
    {
        var showTimeId = Guid.CreateVersion7();
        var customer = DomainTestBuilders.GuestCustomer();
        var booking = DomainTestBuilders.PendingBooking(showTimeId, customer);
        var ticket = DomainTestBuilders.LockingTicket(showTimeId, 40_000m, customer.Id.ToString());

        booking.AddTicket(ticket);

        booking.OriginAmount.Should().Be(40_000m);
    }

    [Fact]
    public void AddTicket_Should_Throw_When_CustomerIsNull()
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = Guid.CreateVersion7(),
            CustomerName = "X",
            Status = BookingStatus.Pending,
            Customer = null
        };
        var ticket = DomainTestBuilders.LockingTicket(booking.ShowTimeId, 1m, "a");

        var act = () => booking.AddTicket(ticket);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Customer information is required*");
    }

    [Fact]
    public void AddTicket_Should_Throw_When_TicketShowTimeDoesNotMatchBooking()
    {
        var customer = DomainTestBuilders.GuestCustomer();
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), customer);
        var ticket = DomainTestBuilders.LockingTicket(Guid.CreateVersion7(), 1m, customer.SessionId);

        var act = () => booking.AddTicket(ticket);
        act.Should().Throw<InvalidOperationException>().WithMessage("*same showtime*");
    }

    [Fact]
    public void AddConcession_Should_IncreaseOriginAmount_When_ConcessionIsAvailable()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var concession = new Concession
        {
            Id = Guid.CreateVersion7(),
            Name = "Popcorn",
            Price = 30_000m,
            IsAvailable = true
        };

        booking.AddConcession(concession, 2);

        booking.OriginAmount.Should().Be(60_000m);
        booking.Concessions.Should().HaveCount(1);
    }

    [Fact]
    public void AddConcession_Should_Throw_When_ConcessionIsNotAvailable()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var concession = new Concession
        {
            Id = Guid.CreateVersion7(),
            Name = "Soda",
            Price = 10_000m,
            IsAvailable = false
        };

        var act = () => booking.AddConcession(concession, 1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddConcession_Should_Throw_When_QuantityIsNotPositive()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var concession = new Concession
        {
            Id = Guid.CreateVersion7(),
            Name = "Soda",
            Price = 10_000m,
            IsAvailable = true
        };

        var act = () => booking.AddConcession(concession, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Confirm_Should_MarkTicketsSoldAndRaiseEvent_When_StatusIsPending()
    {
        var showTimeId = Guid.CreateVersion7();
        var customer = DomainTestBuilders.GuestCustomer();
        var booking = DomainTestBuilders.PendingBooking(showTimeId, customer);
        var ticket = DomainTestBuilders.LockingTicket(showTimeId, 55_000m, customer.SessionId);
        booking.AddTicket(ticket);
        ticket.StartPayment(booking.Id, customer.SessionId, DateTimeOffset.UtcNow.AddMinutes(15));
        booking.UpdateFinalAmount(5_000m);

        booking.Confirm();

        booking.Status.Should().Be(BookingStatus.Confirmed);
        ticket.Status.Should().Be(TicketStatus.Sold);
        ticket.BookingId.Should().Be(booking.Id);
        booking.Events.Should().ContainSingle().Which.Should().BeOfType<BookingConfirmed>();
    }

    [Fact]
    public void Confirm_Should_Throw_When_StatusIsNotPending()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.Status = BookingStatus.Confirmed;

        var act = () => booking.Confirm();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_Should_ReleaseTicketsAndRaiseEvent_When_StatusIsPending()
    {
        var showTimeId = Guid.CreateVersion7();
        var customer = DomainTestBuilders.GuestCustomer();
        var booking = DomainTestBuilders.PendingBooking(showTimeId, customer);
        var ticket = DomainTestBuilders.LockingTicket(showTimeId, 50_000m, customer.SessionId);
        booking.AddTicket(ticket);

        booking.Cancel();

        booking.Status.Should().Be(BookingStatus.Cancelled);
        ticket.Status.Should().Be(TicketStatus.Available);
        booking.Events.Should().ContainSingle().Which.Should().BeOfType<BookingCancelled>();
    }

    [Fact]
    public void Cancel_Should_Throw_When_StatusIsAlreadyCancelled()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.Status = BookingStatus.Cancelled;

        var act = () => booking.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_Should_Throw_When_StatusIsCheckedIn()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.Status = BookingStatus.CheckedIn;

        var act = () => booking.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CheckIn_Should_Throw_When_StatusIsNotConfirmed()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());

        var act = () => booking.CheckIn();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CheckIn_Should_SetCheckedInWithEvent_When_StatusIsConfirmed()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.Status = BookingStatus.Confirmed;

        booking.CheckIn();

        booking.Status.Should().Be(BookingStatus.CheckedIn);
        booking.Events.Should().Contain(e => e is BookingCheckedIn);
    }

    [Theory]
    [InlineData(100, 30, 70)]
    [InlineData(100, 100, 0)]
    [InlineData(100, 150, 0)]
    public void UpdateFinalAmount_Should_ClampToZero_When_DiscountMeetsOrExceedsOrigin(
        decimal origin,
        decimal discount,
        decimal expectedFinal)
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.OriginAmount = origin;

        booking.UpdateFinalAmount(discount);

        booking.FinalAmount.Should().Be(expectedFinal);
    }

    [Fact]
    public void UpdateFinalAmount_Should_Throw_When_DiscountIsNegative()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var act = () => booking.UpdateFinalAmount(-1);
        act.Should().Throw<ArgumentException>();
    }

    // =============================================
    // Coupon-related tests
    // =============================================

    [Fact]
    public void ApplyCoupon_Should_SetCouponCodeAndDiscount()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.OriginAmount = 200_000m;

        booking.ApplyCoupon("KOL50K", 50_000m);

        booking.CouponCode.Should().Be("KOL50K");
        booking.CouponDiscountAmount.Should().Be(50_000m);
    }

    [Fact]
    public void ApplyCoupon_Should_Throw_When_CodeEmpty()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var act = () => booking.ApplyCoupon("", 10_000m);
        act.Should().Throw<ArgumentException>().WithParameterName("couponCode");
    }

    [Fact]
    public void ApplyCoupon_Should_Throw_When_DiscountNegative()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var act = () => booking.ApplyCoupon("TEST", -1m);
        act.Should().Throw<ArgumentException>().WithParameterName("discountAmount");
    }

    [Fact]
    public void ApplyCoupon_Should_Raise_CouponAppliedEvent()
    {
        var customerId = Guid.CreateVersion7();
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.CustomerId = customerId;
        booking.OriginAmount = 200_000m;

        booking.ApplyCoupon("KOL50K", 50_000m);

        booking.Events.Should().ContainSingle(e => e is CouponApplied);
        var ev = (CouponApplied)booking.Events.First(e => e is CouponApplied);
        ev.CouponCode.Should().Be("KOL50K");
        ev.DiscountAmount.Should().Be(50_000m);
        ev.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public void UpdateFinalAmount_WithTwoOverloads_Should_BothReduceCorrectly()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.OriginAmount = 280_000m;

        // Apply coupon first (simulates real flow)
        booking.ApplyCoupon("VICINEMA10", 28_000m);
        booking.UpdateFinalAmount(22_000m, 28_000m);

        booking.FinalAmount.Should().Be(230_000m); // 280k - 22k - 28k
    }

    [Fact]
    public void UpdateFinalAmount_LegacyOverload_Should_UseCouponDiscountAmount()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.OriginAmount = 200_000m;
        booking.CouponDiscountAmount = 30_000m;

        booking.UpdateFinalAmount(20_000m); // legacy overload

        booking.FinalAmount.Should().Be(150_000m); // 200k - 20k - 30k
    }

    [Fact]
    public void UpdateFinalAmount_Should_Throw_When_CouponDiscountNegative()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var act = () => booking.UpdateFinalAmount(0m, -1m);
        act.Should().Throw<ArgumentException>().WithParameterName("couponDiscount");
    }

    [Fact]
    public void ApplyPromotion_Should_Raise_PromotionAppliedEvent()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        var customerId = Guid.CreateVersion7();
        booking.CustomerId = customerId;

        var promotion = BookingPromotion.Create(
            bookingId: booking.Id,
            promotionProgramId: Guid.CreateVersion7(),
            name: "Summer Sale",
            discountAmount: 15_000m,
            discountType: PromotionDiscountType.OrderTotal);

        booking.ApplyPromotion(promotion);

        booking.Events.Should().ContainSingle(e => e is PromotionApplied);
        var ev = (PromotionApplied)booking.Events.First(e => e is PromotionApplied);
        ev.PromotionProgramId.Should().Be(promotion.PromotionProgramId);
        ev.BookingId.Should().Be(booking.Id);
        ev.CustomerId.Should().Be(customerId);
        ev.PromotionName.Should().Be("Summer Sale");
        ev.DiscountAmount.Should().Be(15_000m);
        ev.DiscountType.Should().Be(PromotionDiscountType.OrderTotal);
    }

    [Fact]
    public void CouponAndLoyaltyDiscount_Should_Stack_UpToZero()
    {
        var booking = DomainTestBuilders.PendingBooking(Guid.CreateVersion7(), DomainTestBuilders.GuestCustomer());
        booking.OriginAmount = 100_000m;

        booking.UpdateFinalAmount(80_000m, 80_000m); // total discount 160k > origin 100k

        booking.FinalAmount.Should().Be(0m);
    }
}

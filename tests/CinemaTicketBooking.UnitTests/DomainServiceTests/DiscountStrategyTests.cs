using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Services;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.DomainServiceTests;

/// <summary>
/// Unit tests for DiscountStrategyComposite and the strategy pattern.
/// </summary>
public class DiscountStrategyTests
{
    private static Booking BookingWithOrigin(decimal origin)
    {
        return new Booking
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = Guid.CreateVersion7(),
            CustomerName = "Test",
            Status = BookingStatus.Pending,
            OriginAmount = origin,
            FinalAmount = 0m,
            Customer = null,
        };
    }

    // =============================================
    // LoyaltyTierDiscountStrategy
    // =============================================

    [Fact]
    public void LoyaltyTierStrategy_Should_ReturnNull_When_CustomerIsGuest()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Guest",
            IsRegistered = false,
            LoyaltyTier = LoyaltyTier.Bronze
        };
        var booking = BookingWithOrigin(200_000m);
        var strategy = new LoyaltyTierDiscountStrategy(new LoyaltyDiscountService());

        var result = strategy.Calculate(booking, customer,
        [
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10)
        ]);

        result.Should().BeNull();
    }

    [Fact]
    public void LoyaltyTierStrategy_Should_ReturnNull_When_NoMatchingTier()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "User",
            IsRegistered = true,
            LoyaltyTier = LoyaltyTier.Gold
        };
        var booking = BookingWithOrigin(200_000m);
        var strategy = new LoyaltyTierDiscountStrategy(new LoyaltyDiscountService());

        var result = strategy.Calculate(booking, customer,
        [
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10)
        ]);

        result.Should().BeNull();
    }

    [Fact]
    public void LoyaltyTierStrategy_Should_CalculateBronzeDiscount()
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = "Bronze User",
            SessionId = "bronze-session",
            IsRegistered = true,
            LoyaltyTier = LoyaltyTier.Bronze
        };

        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = Guid.CreateVersion7(),
            CustomerName = "Test",
            Status = BookingStatus.Pending,
            OriginAmount = 0m,
            FinalAmount = 0m,
            Customer = customer,
            CustomerId = customer.Id,
        };
        booking.AddConcession(new Concession { Id = Guid.NewGuid(), Name = "Popcorn", Price = 50_000m, IsAvailable = true }, 1);
        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            ShowTimeId = booking.ShowTimeId,
            Price = 250_000m,
            Status = TicketStatus.Locking,
            Code = "T1",
            SeatCode = "A1",
            LockingBy = "bronze-session",
            LockExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        booking.AddTicket(ticket);

        var strategy = new LoyaltyTierDiscountStrategy(new LoyaltyDiscountService());
        var result = strategy.Calculate(booking, customer,
        [
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10)
        ]);

        result.Should().NotBeNull();
        result!.TicketDiscount.Should().Be(0m); // Bronze: 0% ticket
        result.ConcessionDiscount.Should().Be(5_000m); // Bronze: 10% concession on 50k = 5k
    }

    // =============================================
    // DiscountStrategyComposite
    // =============================================

    [Fact]
    public void Composite_Should_SumMultipleStrategies()
    {
        // Fake strategy that always returns 10k
        var fake1 = new FakeDiscountStrategy(1, 5_000m, 3_000m);
        var fake2 = new FakeDiscountStrategy(2, 2_000m, 1_000m);

        var composite = new DiscountStrategyComposite([fake1, fake2]);
        var booking = BookingWithOrigin(200_000m);
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test", IsRegistered = true };

        var total = composite.CalculateTotalDiscount(booking, customer,
        [
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10)
        ]);

        total.Should().Be(11_000m); // (5000+3000) + (2000+1000) = 8000+3000 = 11000
    }

    [Fact]
    public void Composite_Should_ExecuteInPriorityOrder()
    {
        var called = new List<int>();
        var strategy1 = new OrderedFakeStrategy(0, () => called.Add(1));
        var strategy2 = new OrderedFakeStrategy(5, () => called.Add(2));
        var strategy3 = new OrderedFakeStrategy(2, () => called.Add(3));

        var composite = new DiscountStrategyComposite([strategy1, strategy2, strategy3]);
        var booking = BookingWithOrigin(100_000m);
        var customer = new Customer { Id = Guid.NewGuid(), Name = "Test", IsRegistered = true };

        composite.CalculateTotalDiscount(booking, customer, []);

        called.Should().Equal([1, 3, 2]); // priority order: 0, 2, 5
    }

    // =============================================
    // Test fakes
    // =============================================

    private sealed class FakeDiscountStrategy(int priority, decimal ticketDiscount, decimal concessionDiscount) : IDiscountStrategy
    {
        public int Priority => priority;

        public LoyaltyDiscountResult? Calculate(Booking booking, Customer customer, IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
            => new(ticketDiscount, concessionDiscount);
    }

    private sealed class OrderedFakeStrategy(int priority, Action recordCall) : IDiscountStrategy
    {
        public int Priority => priority;

        public LoyaltyDiscountResult? Calculate(Booking booking, Customer customer, IReadOnlyList<LoyaltyTierConfiguration> activeTiers)
        {
            recordCall();
            return null;
        }
    }
}
using CinemaTicketBooking.Domain;
using CinemaTicketBooking.Domain.Services;
using FluentAssertions;

namespace CinemaTicketBooking.UnitTests.DomainServiceTests;

/// <summary>
/// Unit tests for LoyaltyDiscountService: points calculation, discount calculation, tier determination.
/// </summary>
public class LoyaltyDiscountServiceTests
{
    private readonly LoyaltyDiscountService _service = new();

    // =============================================
    // CalculatePoints
    // =============================================

    [Theory]
    [InlineData(10000, 10)]
    [InlineData(50000, 50)]
    [InlineData(999, 0)]
    [InlineData(1000, 1)]
    [InlineData(1500, 1)]
    [InlineData(0, 0)]
    public void CalculatePoints_Should_ConvertAmountToPoints(decimal amount, int expectedPoints)
    {
        var points = _service.CalculatePoints(amount);
        points.Should().Be(expectedPoints);
    }

    // =============================================
    // DetermineTier helpers
    // =============================================

    private static List<LoyaltyTierConfiguration> AllTiers()
    {
        return
        [
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Silver, "Bạc", 101, 500, 5, 15),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Gold, "Vàng", 501, 1500, 10, 20),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Platinum, "Bạch Kim", 1501, 5000, 15, 25),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Diamond, "Kim Cương", 5001, 15000, 20, 35),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Ruby, "Ruby", 15001, null, 30, 50)
        ];
    }

    [Theory]
    [InlineData(0, LoyaltyTier.Bronze)]
    [InlineData(50, LoyaltyTier.Bronze)]
    [InlineData(100, LoyaltyTier.Bronze)]
    [InlineData(101, LoyaltyTier.Silver)]
    [InlineData(500, LoyaltyTier.Silver)]
    [InlineData(501, LoyaltyTier.Gold)]
    [InlineData(1500, LoyaltyTier.Gold)]
    [InlineData(1501, LoyaltyTier.Platinum)]
    [InlineData(5000, LoyaltyTier.Platinum)]
    [InlineData(5001, LoyaltyTier.Diamond)]
    [InlineData(15000, LoyaltyTier.Diamond)]
    [InlineData(15001, LoyaltyTier.Ruby)]
    [InlineData(99999, LoyaltyTier.Ruby)]
    public void DetermineTier_Should_ReturnCorrectTier(int points, LoyaltyTier expectedTier)
    {
        var tiers = AllTiers();
        var tier = _service.DetermineTier(points, tiers);
        tier.Should().Be(expectedTier);
    }

    [Fact]
    public void DetermineTier_Should_HandleUnsortedTiers()
    {
        var tiers = new List<LoyaltyTierConfiguration>
        {
            LoyaltyTierConfiguration.Create(LoyaltyTier.Gold, "Vàng", 501, 1500, 10, 20),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Ruby, "Ruby", 15001, null, 30, 50),
            LoyaltyTierConfiguration.Create(LoyaltyTier.Silver, "Bạc", 101, 500, 5, 15),
        };
        var tier = _service.DetermineTier(600, tiers);
        tier.Should().Be(LoyaltyTier.Gold);
    }

    // =============================================
    // CalculateDiscount
    // =============================================

    [Fact]
    public void CalculateDiscount_Should_ComputeTicketAndConcessionDiscounts()
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = Guid.CreateVersion7(),
            CustomerName = "Test",
            Tickets =
            [
                new BookingTicket
                {
                    Id = Guid.CreateVersion7(),
                    Ticket = new Ticket { Price = 100000m, Status = TicketStatus.Locking }
                },
                new BookingTicket
                {
                    Id = Guid.CreateVersion7(),
                    Ticket = new Ticket { Price = 100000m, Status = TicketStatus.Locking }
                }
            ],
            Concessions =
            [
                new BookingConcession
                {
                    Id = Guid.CreateVersion7(),
                    Concession = new Concession { Name = "Popcorn", Price = 50000m },
                    Quantity = 2
                }
            ]
        };
        var customer = new Customer { IsRegistered = true, LoyaltyTier = LoyaltyTier.Gold };
        var tierConfig = LoyaltyTierConfiguration.Create(LoyaltyTier.Gold, "Vàng", 501, 1500, 10, 20);

        // Act
        var result = _service.CalculateDiscount(booking, customer, tierConfig);

        // Ticket discount: 200000 * 10% = 20000
        result.TicketDiscount.Should().Be(20000m);
        // Concession discount: 100000 * 20% = 20000
        result.ConcessionDiscount.Should().Be(20000m);
    }

    [Fact]
    public void CalculateDiscount_Should_ReturnZero_When_NoDiscountPercent()
    {
        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            ShowTimeId = Guid.CreateVersion7(),
            CustomerName = "Test",
            Tickets =
            [
                new BookingTicket
                {
                    Id = Guid.CreateVersion7(),
                    Ticket = new Ticket { Price = 100000m, Status = TicketStatus.Locking }
                }
            ],
            Concessions = []
        };
        var customer = new Customer { IsRegistered = true, LoyaltyTier = LoyaltyTier.Bronze };
        var tierConfig = LoyaltyTierConfiguration.Create(LoyaltyTier.Bronze, "Đồng", 0, 100, 0, 10);

        var result = _service.CalculateDiscount(booking, customer, tierConfig);

        result.TicketDiscount.Should().Be(0m);
        result.ConcessionDiscount.Should().Be(0m);
    }
}

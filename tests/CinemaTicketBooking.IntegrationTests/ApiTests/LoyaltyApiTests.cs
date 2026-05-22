using System.Net;
using System.Net.Http.Json;

namespace CinemaTicketBooking.IntegrationTests.ApiTests;

/// <summary>
/// Alba integration tests for loyalty endpoints.
/// </summary>
[Collection(nameof(AuthAlbaCollection))]
public sealed class LoyaltyApiTests(AuthAlbaFixture fixture)
{
    // =============================================
    // GET /api/loyalty/tiers (public)
    // =============================================

    [Fact]
    public async Task GetActiveTiers_Should_Return_ActiveTierList()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/loyalty/tiers");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<object>>();
        body.Should().NotBeNull();
        body!.Count.Should().BeGreaterThan(0);
    }

    // =============================================
    // GET /api/loyalty/me (authenticated)
    // =============================================

    [Fact]
    public async Task GetMyLoyalty_Should_Return401_When_Unauthenticated()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync("/api/loyalty/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

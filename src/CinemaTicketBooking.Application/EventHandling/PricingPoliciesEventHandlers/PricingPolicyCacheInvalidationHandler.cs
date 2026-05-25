using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class PricingPolicyCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(PricingPolicyCreated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PricingPolicyId, ct);
    public async Task Handle(PricingPolicyBasicInfoUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PricingPolicyId, ct);
    public async Task Handle(PricingPolicyDeleted @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PricingPolicyId, ct);

    private async Task InvalidateCacheAsync(Guid pricingPolicyId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(PricingPolicyCacheKeys.GetPricingPolicyById(pricingPolicyId), ct);
        await cacheService.RemoveByPrefix(PricingPolicyCacheKeys.ListPrefix, ct);
    }
}

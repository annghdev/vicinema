using CinemaTicketBooking.Application.Abstractions;
using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Application.Features;

public class SeatSelectionPolicyCacheInvalidationHandler(ICacheService cacheService)
{
    public async Task Handle(SeatSelectionPolicyCreated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PolicyId, ct);
    public async Task Handle(SeatSelectionPolicyUpdated @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PolicyId, ct);
    public async Task Handle(SeatSelectionPolicyDeleted @event, CancellationToken ct) => await InvalidateCacheAsync(@event.PolicyId, ct);

    private async Task InvalidateCacheAsync(Guid policyId, CancellationToken ct)
    {
        await cacheService.RemoveAsync(SeatSelectionPolicyCacheKeys.GetSeatSelectionPolicyById(policyId), ct);
        await cacheService.RemoveByPrefix(SeatSelectionPolicyCacheKeys.ListPrefix, ct);
    }
}

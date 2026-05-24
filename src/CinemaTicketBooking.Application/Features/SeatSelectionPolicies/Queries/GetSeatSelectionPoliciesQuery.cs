using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets all seat selection policies.
/// </summary>
public class GetSeatSelectionPoliciesQuery : ICachableQuery<IReadOnlyList<SeatSelectionPolicyDto>>
{
    public bool? IsActive { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => SeatSelectionPolicyCacheKeys.GetSeatSelectionPolicies(IsActive);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for all seat selection policies.
/// </summary>
public class GetSeatSelectionPoliciesHandler(IUnitOfWork uow)
{
    public async Task<IReadOnlyList<SeatSelectionPolicyDto>> Handle(GetSeatSelectionPoliciesQuery query, CancellationToken ct)
    {
        var items = await uow.SeatSelectionPolicies
            .GetQueryFilter()
            .OrderByDescending(x => x.IsGlobalDefault)
            .ThenBy(x => x.Name)
            .Select(x => new SeatSelectionPolicyDto(
                x.Id,
                x.Name,
                x.IsGlobalDefault,
                x.IsActive,
                x.MaxTicketsPerCheckout,
                x.MaxRowsPerCheckout,
                x.OrphanSeatLevel,
                x.CheckerboardLevel,
                x.SplitAcrossAisleLevel,
                x.IsolatedRowEndSingleLevel,
                x.MisalignedRowsLevel,
                x.CreatedAt))
            .ToListAsync(ct);

        return items;
    }
}

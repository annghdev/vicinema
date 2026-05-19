using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets a seat selection policy by id.
/// </summary>
public class GetSeatSelectionPolicyByIdQuery : ICachableQuery<SeatSelectionPolicyDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => SeatSelectionPolicyCacheKeys.GetSeatSelectionPolicyById(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for a specific seat selection policy.
/// </summary>
public class GetSeatSelectionPolicyByIdHandler(IUnitOfWork uow)
{
    public async Task<SeatSelectionPolicyDto?> Handle(GetSeatSelectionPolicyByIdQuery query, CancellationToken ct)
    {
        var item = await uow.SeatSelectionPolicies
            .GetQueryFilter()
            .Where(x => x.Id == query.Id)
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
            .FirstOrDefaultAsync(ct);

        return item;
    }
}

public class GetSeatSelectionPolicyByIdValidator : AbstractValidator<GetSeatSelectionPolicyByIdQuery>
{
    public GetSeatSelectionPolicyByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID is required.");
    }
}

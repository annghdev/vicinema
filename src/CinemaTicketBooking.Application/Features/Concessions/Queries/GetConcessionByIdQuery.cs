using Microsoft.EntityFrameworkCore;

namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets a concession item by id.
/// </summary>
public class GetConcessionByIdQuery : ICachableQuery<ConcessionDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => ConcessionCacheKeys.GetConcessionById(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for a specific concession item.
/// </summary>
public class GetConcessionByIdHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns concession data when found; otherwise null.
    /// </summary>
    public async Task<ConcessionDto?> Handle(GetConcessionByIdQuery query, CancellationToken ct)
    {
        var concession = await uow.Concessions
            .GetQueryFilter()
            .Where(x => x.Id == query.Id)
            .Select(x => new ConcessionDto(
                x.Id,
                x.Name,
                x.Price,
                x.ImageUrl,
                x.IsAvailable,
                x.CreatedAt))
            .FirstOrDefaultAsync(ct);

        return concession;
    }
}

/// <summary>
/// Validates query payload.
/// </summary>
public class GetConcessionByIdValidator : AbstractValidator<GetConcessionByIdQuery>
{
    public GetConcessionByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Concession ID is required.");
    }
}

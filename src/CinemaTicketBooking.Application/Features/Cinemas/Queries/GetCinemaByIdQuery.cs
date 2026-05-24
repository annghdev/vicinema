namespace CinemaTicketBooking.Application.Features;

/// <summary>
/// Gets a cinema by id.
/// </summary>
public class GetCinemaByIdQuery : ICachableQuery<CinemaDto?>
{
    public Guid Id { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string CacheKey => CinemaCacheKeys.GetCinemaById(Id);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(10);
}

/// <summary>
/// Handles query for a specific cinema.
/// </summary>
public class GetCinemaByIdHandler(IUnitOfWork uow)
{
    /// <summary>
    /// Returns cinema data when found; otherwise null.
    /// </summary>
    public async Task<CinemaDto?> Handle(GetCinemaByIdQuery query, CancellationToken ct)
    {
        var cinema = await uow.Cinemas.GetByIdAsync(query.Id, ct);
        if (cinema is null)
        {
            return null;
        }

        return ToDto(cinema);
    }

    private static CinemaDto ToDto(Cinema cinema)
    {
        return new CinemaDto(
            cinema.Id,
            cinema.Name,
            cinema.ThumbnailUrl,
            cinema.Geo,
            cinema.Address,
            cinema.IsActive,
            cinema.CreatedAt
        );
    }
}

/// <summary>
/// Validates query payload.
/// </summary>
public class GetCinemaByIdValidator : AbstractValidator<GetCinemaByIdQuery>
{
    public GetCinemaByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Cinema ID is required.");
    }
}

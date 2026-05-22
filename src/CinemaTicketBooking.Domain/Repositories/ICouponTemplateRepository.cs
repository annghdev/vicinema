namespace CinemaTicketBooking.Domain.Repositories;

/// <summary>
/// Repository for <see cref="CouponTemplate"/>.
/// </summary>
public interface ICouponTemplateRepository : IRepository<CouponTemplate>
{
    /// <summary>
    /// Finds a coupon template by its unique code.
    /// </summary>
    Task<CouponTemplate?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Returns all active public templates.
    /// </summary>
    Task<IReadOnlyList<CouponTemplate>> GetActivePublicTemplatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all templates ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<CouponTemplate>> GetAllOrderedAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns all personal-type templates.
    /// </summary>
    Task<IReadOnlyList<CouponTemplate>> GetPersonalTemplatesAsync(CancellationToken ct = default);
}
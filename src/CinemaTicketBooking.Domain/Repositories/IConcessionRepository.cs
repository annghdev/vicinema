namespace CinemaTicketBooking.Domain;

/// <summary>
/// Repository interface for the Concession entity.
/// Provides CRUD operations for snack and drink items available at the cinema.
/// </summary>
public interface IConcessionRepository : IRepository<Concession>
{
    /// <summary>
    /// Efficiently loads multiple concessions by their IDs in a single database round-trip.
    /// Used to avoid N+1 queries when resolving free items or batch concession lookups.
    /// </summary>
    Task<List<Concession>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}

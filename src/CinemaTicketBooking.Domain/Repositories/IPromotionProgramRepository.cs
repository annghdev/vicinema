using CinemaTicketBooking.Domain;

namespace CinemaTicketBooking.Domain.Repositories;

public interface IPromotionProgramRepository : IRepository<PromotionProgram>
{
    Task<IReadOnlyList<PromotionProgram>> GetActiveWithConditionsAsync(CancellationToken ct = default);
    Task<PromotionProgram?> GetByIdWithConditionsAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PromotionProgram>> GetAllWithConditionsAsync(CancellationToken ct = default);
}

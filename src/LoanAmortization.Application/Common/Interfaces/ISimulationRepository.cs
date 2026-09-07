using LoanAmortization.Domain.Entities;

namespace LoanAmortization.Application.Common.Interfaces;

public interface ISimulationRepository
{
    Task<Simulation?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Simulation>> ListByUserAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Simulation>> ListByIdsAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default);
    Task AddAsync(Simulation simulation, CancellationToken ct = default);
}

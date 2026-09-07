using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Entities;
using LoanAmortization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanAmortization.Infrastructure.Repositories;

public class EfSimulationRepository(AppDbContext db) : ISimulationRepository
{
    public Task<Simulation?> FindByIdAsync(Guid id, CancellationToken ct = default)
        => db.Simulations.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Simulation>> ListByUserAsync(Guid userId, CancellationToken ct = default)
        => await db.Simulations
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Simulation>> ListByIdsAsync(IEnumerable<Guid> ids, Guid userId, CancellationToken ct = default)
        => await db.Simulations
            .Where(s => ids.Contains(s.Id) && s.UserId == userId)
            .ToListAsync(ct);

    public async Task AddAsync(Simulation simulation, CancellationToken ct = default)
    {
        await db.Simulations.AddAsync(simulation, ct);
        await db.SaveChangesAsync(ct);
    }
}

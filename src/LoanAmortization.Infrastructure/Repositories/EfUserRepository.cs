using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Entities;
using LoanAmortization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LoanAmortization.Infrastructure.Repositories;

public class EfUserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await db.Users.AddAsync(user, ct);
        await db.SaveChangesAsync(ct);
    }
}

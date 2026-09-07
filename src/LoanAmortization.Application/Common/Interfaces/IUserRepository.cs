using LoanAmortization.Domain.Entities;

namespace LoanAmortization.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
}

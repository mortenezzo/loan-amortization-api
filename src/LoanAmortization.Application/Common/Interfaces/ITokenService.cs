using LoanAmortization.Domain.Entities;

namespace LoanAmortization.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}

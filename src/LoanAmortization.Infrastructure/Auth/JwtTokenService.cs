using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LoanAmortization.Infrastructure.Auth;

public class JwtTokenService(IConfiguration config) : ITokenService
{
    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(config["Jwt:SecretKey"]!));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            ],
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(config["Jwt:ExpirationMinutes"]!)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

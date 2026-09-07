using FluentValidation;
using LoanAmortization.Application.Common.Exceptions;
using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Entities;
using MediatR;

namespace LoanAmortization.Application.Auth;

public record RegisterCommand(string Email, string Password) : IRequest<RegisterResult>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class RegisterHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        if (await userRepository.FindByEmailAsync(request.Email, ct) is not null)
            throw new EmailAlreadyExistsException();

        var hash = passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, hash);
        await userRepository.AddAsync(user, ct);

        var token = tokenService.GenerateToken(user);
        return new RegisterResult(user.Id, token);
    }
}

public record RegisterResult(Guid UserId, string Token);

using FluentValidation;
using LoanAmortization.Application.Common.Exceptions;
using LoanAmortization.Application.Common.Interfaces;
using MediatR;

namespace LoanAmortization.Application.Auth;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler(
    IUserRepository userRepository,
    ITokenService tokenService,
    IPasswordHasher passwordHasher) : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await userRepository.FindByEmailAsync(request.Email, ct)
            ?? throw new InvalidCredentialsException();

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidCredentialsException();

        var token = tokenService.GenerateToken(user);
        return new LoginResult(user.Id, token);
    }
}

public record LoginResult(Guid UserId, string Token);

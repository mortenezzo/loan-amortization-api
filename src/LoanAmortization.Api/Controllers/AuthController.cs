using LoanAmortization.Api.DTOs;
using LoanAmortization.Application.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LoanAmortization.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterCommand(request.Email, request.Password), ct);
        return StatusCode(201, new AuthResponse(result.Token, result.UserId));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), ct);
        return Ok(new AuthResponse(result.Token, result.UserId));
    }
}

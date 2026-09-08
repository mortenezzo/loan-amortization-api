using LoanAmortization.Api.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace LoanAmortization.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new HealthResponseDto("healthy", DateTime.UtcNow));
}

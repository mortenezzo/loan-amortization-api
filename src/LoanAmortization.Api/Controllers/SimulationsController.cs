using System.Security.Claims;
using LoanAmortization.Api.DTOs;
using LoanAmortization.Application.Simulations;
using LoanAmortization.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanAmortization.Api.Controllers;

[ApiController]
[Route("api/v1")]
public class SimulationsController(ISender sender) : ControllerBase
{
    [HttpPost("simulate")]
    public async Task<IActionResult> Simulate([FromBody] SimulateRequest request, CancellationToken ct)
    {
        Guid? userId = null;
        if (request.Save)
        {
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sub is null) return Unauthorized();
            userId = Guid.Parse(sub);
        }

        var result = await sender.Send(
            new SimulateCommand(request.Currency, request.Principal, request.AnnualRate, request.TermMonths, request.Save, userId), ct);

        return Ok(ToSimulateResponse(result));
    }

    [HttpGet("simulations")]
    [Authorize]
    public async Task<IActionResult> ListSimulations(CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await sender.Send(new ListSimulationsQuery(userId), ct);
        return Ok(result.Simulations.Select(ToSummaryDto).ToList());
    }

    [HttpGet("simulations/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetSimulation(Guid id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await sender.Send(new GetSimulationQuery(id, userId), ct);
        return Ok(ToDetailDto(result));
    }

    [HttpPost("simulations/compare")]
    [Authorize]
    public async Task<IActionResult> Compare([FromBody] CompareSimulationsRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await sender.Send(new CompareCommand(request.SimulationIds, userId), ct);
        return Ok(new CompareSimulationsResponseDto(result.Simulations.Select(ToSummaryDto).ToList()));
    }

    private Guid GetUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static SimulateResponseDto ToSimulateResponse(SimulateResult r)
        => new(r.SimulationId, r.Currency, r.Principal, r.AnnualRate, r.TermMonths,
               r.MonthlyPayment, r.TotalPayment, r.TotalInterest,
               r.Schedule.Select(ToRowDto).ToList());

    private static SimulationDetailDto ToDetailDto(GetSimulationResult r)
        => new(r.Summary.Id, r.Summary.Currency, r.Summary.Principal, r.Summary.AnnualRate,
               r.Summary.TermMonths, r.Summary.MonthlyPayment, r.Summary.TotalPayment,
               r.Summary.TotalInterest, r.Summary.CreatedAt,
               r.Schedule.Select(ToRowDto).ToList());

    private static SimulationSummaryDto ToSummaryDto(SimulationSummaryData s)
        => new(s.Id, s.Currency, s.Principal, s.AnnualRate, s.TermMonths,
               s.MonthlyPayment, s.TotalPayment, s.TotalInterest, s.CreatedAt);

    private static InstallmentRowDto ToRowDto(InstallmentRow r)
        => new(r.Month, Math.Round(r.Payment, 2), Math.Round(r.Interest, 2),
               Math.Round(r.Capital, 2), Math.Round(r.RemainingBalance, 2));
}

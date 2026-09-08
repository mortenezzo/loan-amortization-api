using LoanAmortization.Application.Common.Exceptions;
using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Services;
using LoanAmortization.Domain.ValueObjects;
using MediatR;

namespace LoanAmortization.Application.Simulations;

public record GetSimulationQuery(Guid Id, Guid UserId) : IRequest<GetSimulationResult>;

public class GetSimulationHandler(
    ISimulationRepository simulationRepository,
    AmortizationCalculator calculator) : IRequestHandler<GetSimulationQuery, GetSimulationResult>
{
    public async Task<GetSimulationResult> Handle(GetSimulationQuery request, CancellationToken ct)
    {
        var simulation = await simulationRepository.FindByIdAsync(request.Id, ct)
            ?? throw new SimulationNotFoundException();

        if (simulation.UserId != request.UserId)
            throw new SimulationForbiddenException();

        var schedule = calculator.Calculate(simulation.Principal, simulation.AnnualRate, simulation.TermMonths);
        var mp = Math.Round(schedule[0].Payment, 2);
        var tp = Math.Round(schedule.Sum(r => r.Payment), 2);

        var summary = new SimulationSummaryData(
            simulation.Id, simulation.Currency, simulation.Principal,
            simulation.AnnualRate, simulation.TermMonths,
            mp, tp, tp - simulation.Principal, simulation.CreatedAt);

        return new GetSimulationResult(summary, schedule);
    }
}

public record GetSimulationResult(SimulationSummaryData Summary, IReadOnlyList<InstallmentRow> Schedule);

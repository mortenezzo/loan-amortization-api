using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Services;
using MediatR;

namespace LoanAmortization.Application.Simulations;

public record ListSimulationsQuery(Guid UserId) : IRequest<ListSimulationsResult>;

public class ListSimulationsHandler(
    ISimulationRepository simulationRepository,
    AmortizationCalculator calculator) : IRequestHandler<ListSimulationsQuery, ListSimulationsResult>
{
    public async Task<ListSimulationsResult> Handle(ListSimulationsQuery request, CancellationToken ct)
    {
        var simulations = await simulationRepository.ListByUserAsync(request.UserId, ct);

        var summaries = simulations
            .Select(s =>
            {
                var schedule = calculator.Calculate(s.Principal, s.AnnualRate, s.TermMonths);
                var mp = Math.Round(schedule[0].Payment, 2);
                var tp = Math.Round(schedule.Sum(r => r.Payment), 2);
                return new SimulationSummaryData(s.Id, s.Currency, s.Principal, s.AnnualRate, s.TermMonths, mp, tp, tp - s.Principal, s.CreatedAt);
            })
            .ToList();

        return new ListSimulationsResult(summaries);
    }
}

public record ListSimulationsResult(IReadOnlyList<SimulationSummaryData> Simulations);

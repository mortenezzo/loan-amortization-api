using FluentValidation;
using LoanAmortization.Application.Common.Exceptions;
using LoanAmortization.Application.Common.Interfaces;
using LoanAmortization.Domain.Services;
using MediatR;

namespace LoanAmortization.Application.Simulations;

public record CompareCommand(IReadOnlyList<Guid> SimulationIds, Guid UserId) : IRequest<CompareResult>;

public class CompareCommandValidator : AbstractValidator<CompareCommand>
{
    public CompareCommandValidator()
    {
        RuleFor(x => x.SimulationIds)
            .NotEmpty()
            .Must(ids => ids.Count >= 2 && ids.Count <= 10)
            .WithMessage("Debe enviar entre 2 y 10 IDs de simulación.");
    }
}

public class CompareHandler(
    ISimulationRepository simulationRepository,
    AmortizationCalculator calculator) : IRequestHandler<CompareCommand, CompareResult>
{
    public async Task<CompareResult> Handle(CompareCommand request, CancellationToken ct)
    {
        var simulations = await simulationRepository.ListByIdsAsync(request.SimulationIds, request.UserId, ct);

        if (simulations.Count != request.SimulationIds.Count)
            throw new SimulationNotFoundException();

        var lookup = simulations.ToDictionary(s => s.Id);

        var summaries = request.SimulationIds
            .Select(id =>
            {
                var s = lookup[id];
                var schedule = calculator.Calculate(s.Principal, s.AnnualRate, s.TermMonths);
                var mp = Math.Round(schedule[0].Payment, 2);
                var tp = Math.Round(schedule.Sum(r => r.Payment), 2);
                return new SimulationSummaryData(s.Id, s.Currency, s.Principal, s.AnnualRate, s.TermMonths, mp, tp, tp - s.Principal, s.CreatedAt);
            })
            .ToList();

        return new CompareResult(summaries);
    }
}

public record CompareResult(IReadOnlyList<SimulationSummaryData> Simulations);

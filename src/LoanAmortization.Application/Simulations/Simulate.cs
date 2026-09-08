using FluentValidation;
using LoanAmortization.Domain.Entities;
using LoanAmortization.Domain.Services;
using LoanAmortization.Domain.ValueObjects;
using LoanAmortization.Application.Common.Interfaces;
using MediatR;

namespace LoanAmortization.Application.Simulations;

public record SimulateCommand(
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    bool Save,
    Guid? UserId) : IRequest<SimulateResult>;

public class SimulateCommandValidator : AbstractValidator<SimulateCommand>
{
    public SimulateCommandValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Must(c => c == "USD" || c == "PEN")
            .WithMessage("La moneda debe ser USD o PEN.");
        RuleFor(x => x.Principal).GreaterThan(0);
        RuleFor(x => x.AnnualRate).GreaterThanOrEqualTo(0.01m).LessThanOrEqualTo(100m);
        RuleFor(x => x.TermMonths).InclusiveBetween(1, 360);
    }
}

public class SimulateHandler(
    AmortizationCalculator calculator,
    ISimulationRepository simulationRepository) : IRequestHandler<SimulateCommand, SimulateResult>
{
    public async Task<SimulateResult> Handle(SimulateCommand request, CancellationToken ct)
    {
        var schedule = calculator.Calculate(request.Principal, request.AnnualRate, request.TermMonths);

        var monthlyPayment = Math.Round(schedule[0].Payment, 2);
        var totalPayment = Math.Round(schedule.Sum(r => r.Payment), 2);
        var totalInterest = totalPayment - request.Principal;

        Guid? simulationId = null;
        if (request.Save && request.UserId.HasValue)
        {
            var simulation = Simulation.Create(
                request.UserId.Value, request.Currency,
                request.Principal, request.AnnualRate, request.TermMonths);
            await simulationRepository.AddAsync(simulation, ct);
            simulationId = simulation.Id;
        }

        return new SimulateResult(
            simulationId, request.Currency, request.Principal,
            request.AnnualRate, request.TermMonths,
            monthlyPayment, totalPayment, totalInterest, schedule);
    }
}

public record SimulateResult(
    Guid? SimulationId,
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    IReadOnlyList<InstallmentRow> Schedule);

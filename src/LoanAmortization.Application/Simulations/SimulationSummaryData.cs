namespace LoanAmortization.Application.Simulations;

public record SimulationSummaryData(
    Guid Id,
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    DateTime CreatedAt);

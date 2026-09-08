namespace LoanAmortization.Api.DTOs;

public record SimulationSummaryDto(
    Guid Id,
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    DateTime CreatedAt);

namespace LoanAmortization.Api.DTOs;

public record SimulationDetailDto(
    Guid Id,
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    DateTime CreatedAt,
    IReadOnlyList<InstallmentRowDto> Schedule);

namespace LoanAmortization.Api.DTOs;

public record SimulateResponseDto(
    Guid? SimulationId,
    string Currency,
    decimal Principal,
    decimal AnnualRate,
    int TermMonths,
    decimal MonthlyPayment,
    decimal TotalPayment,
    decimal TotalInterest,
    IReadOnlyList<InstallmentRowDto> Schedule);

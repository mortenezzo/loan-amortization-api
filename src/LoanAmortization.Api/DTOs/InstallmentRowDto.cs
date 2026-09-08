namespace LoanAmortization.Api.DTOs;

public record InstallmentRowDto(int Month, decimal Payment, decimal Interest, decimal Capital, decimal RemainingBalance);

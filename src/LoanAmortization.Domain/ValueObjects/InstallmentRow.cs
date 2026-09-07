namespace LoanAmortization.Domain.ValueObjects;

public record InstallmentRow(
    int Month,
    decimal Payment,
    decimal Interest,
    decimal Capital,
    decimal RemainingBalance);

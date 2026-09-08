namespace LoanAmortization.Api.DTOs;

public record SimulateRequest(string Currency, decimal Principal, decimal AnnualRate, int TermMonths, bool Save = false);

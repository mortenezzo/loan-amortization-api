namespace LoanAmortization.Api.DTOs;

public record CompareSimulationsRequest(IReadOnlyList<Guid> SimulationIds);

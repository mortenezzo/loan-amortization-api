namespace LoanAmortization.Api.DTOs;

public record CompareSimulationsResponseDto(IReadOnlyList<SimulationSummaryDto> Simulations);

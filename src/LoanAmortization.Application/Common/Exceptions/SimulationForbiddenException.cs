namespace LoanAmortization.Application.Common.Exceptions;

public sealed class SimulationForbiddenException() : Exception("No tienes acceso a esta simulación.");

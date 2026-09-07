namespace LoanAmortization.Domain.Entities;

public class Simulation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public decimal Principal { get; private set; }
    public decimal AnnualRate { get; private set; }
    public int TermMonths { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User? User { get; private set; }

    private Simulation() { }

    public static Simulation Create(Guid userId, string currency, decimal principal, decimal annualRate, int termMonths)
    {
        return new Simulation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Currency = currency.ToUpperInvariant(),
            Principal = principal,
            AnnualRate = annualRate,
            TermMonths = termMonths,
            CreatedAt = DateTime.UtcNow
        };
    }
}

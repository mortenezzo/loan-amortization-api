using LoanAmortization.Domain.ValueObjects;

namespace LoanAmortization.Domain.Services;

public class AmortizationCalculator
{
    public IReadOnlyList<InstallmentRow> Calculate(decimal principal, decimal annualRate, int termMonths)
    {
        var r = annualRate / 100m / 12m;
        var rows = new List<InstallmentRow>(termMonths);

        decimal pmt = r == 0m
            ? principal / termMonths
            : principal * r / (1m - DecimalPow(1m + r, -termMonths));

        var balance = principal;
        var capitalAccumulated = 0m;

        for (int i = 1; i <= termMonths; i++)
        {
            var interest = balance * r;

            // On the last row, use the exact complement so that sum(capital) == principal exactly,
            // avoiding sub-cent drift from accumulated decimal multiplication rounding.
            var capital = i < termMonths ? pmt - interest : principal - capitalAccumulated;

            balance -= capital;
            capitalAccumulated += capital;

            rows.Add(new InstallmentRow(i, interest + capital, interest, capital, i == termMonths ? 0m : balance));
        }

        return rows;
    }

    // Pure decimal power to avoid double precision loss for financial calculations.
    // Supports negative exponents via reciprocal.
    private static decimal DecimalPow(decimal baseValue, int exponent)
    {
        if (exponent == 0) return 1m;

        if (exponent < 0)
            return 1m / DecimalPow(baseValue, -exponent);

        var result = 1m;
        for (int i = 0; i < exponent; i++)
            result *= baseValue;

        return result;
    }
}

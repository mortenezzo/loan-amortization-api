using FluentAssertions;
using LoanAmortization.Domain.Services;

namespace LoanAmortization.Domain.Tests;

public class AmortizationCalculatorTests
{
    private readonly AmortizationCalculator _calculator = new();

    // ── Standard loan: $10 000 @ 12% annual, 12 months ──────────────────────

    [Fact]
    public void Calculate_StandardLoan_ReturnsCorrectRowCount()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        schedule.Should().HaveCount(12);
    }

    [Fact]
    public void Calculate_StandardLoan_MonthlyPaymentRoundsTo888_49()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        Math.Round(schedule[0].Payment, 2).Should().Be(888.49m);
    }

    [Fact]
    public void Calculate_StandardLoan_SumOfCapitalsEqualsPrincipal()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        schedule.Sum(r => r.Capital).Should().Be(10_000m);
    }

    [Fact]
    public void Calculate_StandardLoan_FinalBalanceIsZero()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        schedule.Last().RemainingBalance.Should().Be(0m);
    }

    [Fact]
    public void Calculate_StandardLoan_MonthNumbersAreSequential()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        schedule.Select(r => r.Month).Should().Equal(Enumerable.Range(1, 12));
    }

    [Fact]
    public void Calculate_StandardLoan_PaymentEqualsInterestPlusCapitalEveryRow()
    {
        var schedule = _calculator.Calculate(10_000m, 12.0m, 12);
        foreach (var row in schedule)
            row.Payment.Should().Be(row.Interest + row.Capital);
    }

    // ── Edge case: 0% annual rate ────────────────────────────────────────────

    [Fact]
    public void Calculate_ZeroRate_InterestIsZeroEveryRow()
    {
        var schedule = _calculator.Calculate(1_200m, 0m, 12);
        schedule.Should().AllSatisfy(r => r.Interest.Should().Be(0m));
    }

    [Fact]
    public void Calculate_ZeroRate_SumOfCapitalsEqualsPrincipal()
    {
        var schedule = _calculator.Calculate(1_200m, 0m, 12);
        schedule.Sum(r => r.Capital).Should().Be(1_200m);
    }

    [Fact]
    public void Calculate_ZeroRate_FinalBalanceIsZero()
    {
        var schedule = _calculator.Calculate(1_200m, 0m, 12);
        schedule.Last().RemainingBalance.Should().Be(0m);
    }

    // ── Edge case: 1-month term ──────────────────────────────────────────────

    [Fact]
    public void Calculate_OneMonthTerm_SingleRowWithCorrectValues()
    {
        var schedule = _calculator.Calculate(1_000m, 12.0m, 1);

        schedule.Should().HaveCount(1);
        Math.Round(schedule[0].Payment, 2).Should().Be(1_010.00m);
        Math.Round(schedule[0].Interest, 2).Should().Be(10.00m);
        Math.Round(schedule[0].Capital, 2).Should().Be(1_000.00m);
        schedule[0].RemainingBalance.Should().Be(0m);
    }
}

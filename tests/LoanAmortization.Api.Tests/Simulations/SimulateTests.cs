using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests.Simulations;

[Collection(nameof(ApiCollection))]
public class SimulateTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Simulate_WithoutSave_Returns200WithFullSchedule()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency = "USD", principal = 10000m, annual_rate = 12m, term_months = 12, save = false },
            ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SimulateResponseDto>(ApiHelper.JsonOpts);
        body!.SimulationId.Should().BeNull();
        body.Currency.Should().Be("USD");
        body.MonthlyPayment.Should().Be(888.49m);
        body.Schedule.Should().HaveCount(12);
        body.Schedule[0].Month.Should().Be(1);
        body.Schedule[^1].RemainingBalance.Should().Be(0);
    }

    [Fact]
    public async Task Simulate_WithSaveWithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency = "USD", principal = 10000m, annual_rate = 12m, term_months = 12, save = true },
            ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Simulate_WithSaveAuthenticated_Returns200WithSimulationId()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency = "PEN", principal = 50000m, annual_rate = 8.5m, term_months = 24, save = true },
            ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SimulateResponseDto>(ApiHelper.JsonOpts);
        body!.SimulationId.Should().NotBeNull();
        body.SimulationId.Should().NotBe(Guid.Empty);
        body.Schedule.Should().HaveCount(24);
    }

    [Theory]
    [InlineData("EUR", 10000, 12, 12)]
    [InlineData("USD", 0, 12, 12)]
    [InlineData("USD", 10000, 0, 12)]
    [InlineData("USD", 10000, 12, 0)]
    [InlineData("USD", 10000, 101, 12)]
    [InlineData("USD", 10000, 12, 361)]
    public async Task Simulate_InvalidInput_Returns400(string currency, decimal principal, decimal annualRate, int termMonths)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency, principal, annual_rate = annualRate, term_months = termMonths, save = false },
            ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

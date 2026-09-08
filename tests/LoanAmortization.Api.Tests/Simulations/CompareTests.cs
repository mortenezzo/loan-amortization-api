using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests.Simulations;

[Collection(nameof(ApiCollection))]
public class CompareTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<Guid> SaveSimulationAsync(decimal principal, decimal annualRate, int termMonths)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency = "USD", principal, annual_rate = annualRate, term_months = termMonths, save = true },
            ApiHelper.JsonOpts);
        var body = await response.Content.ReadFromJsonAsync<SimulateResponseDto>(ApiHelper.JsonOpts);
        return body!.SimulationId!.Value;
    }

    [Fact]
    public async Task Compare_Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/simulations/compare",
            new { simulation_ids = new[] { Guid.NewGuid(), Guid.NewGuid() } },
            ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Compare_TwoOwnSimulations_Returns200WithBothSummaries()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var id1 = await SaveSimulationAsync(10000m, 12m, 12);
        var id2 = await SaveSimulationAsync(20000m, 8m, 24);

        var response = await _client.PostAsJsonAsync("/api/v1/simulations/compare",
            new { simulation_ids = new[] { id1, id2 } }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CompareSimulationsResponseDto>(ApiHelper.JsonOpts);
        body!.Simulations.Should().HaveCount(2);
        body.Simulations.Select(s => s.Id).Should().Contain([id1, id2]);
    }

    [Fact]
    public async Task Compare_NonExistentId_Returns404()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var id1 = await SaveSimulationAsync(10000m, 12m, 12);

        var response = await _client.PostAsJsonAsync("/api/v1/simulations/compare",
            new { simulation_ids = new[] { id1, Guid.NewGuid() } }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Compare_OtherUsersSimulation_Returns404()
    {
        // User1 creates a simulation
        var auth1 = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth1.Token);
        var id1 = await SaveSimulationAsync(10000m, 12m, 12);

        // User2 creates their own and tries to compare with User1's
        var auth2 = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth2.Token);
        var id2 = await SaveSimulationAsync(5000m, 6m, 6);

        var response = await _client.PostAsJsonAsync("/api/v1/simulations/compare",
            new { simulation_ids = new[] { id2, id1 } }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Compare_SingleId_Returns400()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var response = await _client.PostAsJsonAsync("/api/v1/simulations/compare",
            new { simulation_ids = new[] { Guid.NewGuid() } }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}

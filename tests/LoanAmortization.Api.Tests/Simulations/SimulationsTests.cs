using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests.Simulations;

[Collection(nameof(ApiCollection))]
public class SimulationsTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<(AuthResponse Auth, Guid SimulationId)> CreateSavedSimulationAsync()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var response = await _client.PostAsJsonAsync("/api/v1/simulate",
            new { currency = "USD", principal = 10000m, annual_rate = 12m, term_months = 12, save = true },
            ApiHelper.JsonOpts);
        var body = await response.Content.ReadFromJsonAsync<SimulateResponseDto>(ApiHelper.JsonOpts);
        return (auth, body!.SimulationId!.Value);
    }

    [Fact]
    public async Task ListSimulations_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/simulations");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListSimulations_Authenticated_ReturnsOwnSimulations()
    {
        await CreateSavedSimulationAsync();

        var response = await _client.GetAsync("/api/v1/simulations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<SimulationSummaryDto>>(ApiHelper.JsonOpts);
        body.Should().HaveCountGreaterThanOrEqualTo(1);
        body!.All(s => s.Principal > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetSimulation_Unauthenticated_Returns401()
    {
        var response = await _client.GetAsync($"/api/v1/simulations/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSimulation_OwnSimulation_Returns200WithSchedule()
    {
        var (_, simId) = await CreateSavedSimulationAsync();

        var response = await _client.GetAsync($"/api/v1/simulations/{simId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SimulationDetailDto>(ApiHelper.JsonOpts);
        body!.Id.Should().Be(simId);
        body.Schedule.Should().HaveCount(12);
        body.Schedule[^1].RemainingBalance.Should().Be(0);
    }

    [Fact]
    public async Task GetSimulation_NotFound_Returns404()
    {
        var auth = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth.Token);

        var response = await _client.GetAsync($"/api/v1/simulations/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSimulation_OtherUsersSimulation_Returns403()
    {
        // User1 creates a simulation
        var (_, simId) = await CreateSavedSimulationAsync();

        // User2 registers and tries to access it
        var auth2 = await ApiHelper.RegisterAsync(_client, ApiHelper.UniqueEmail(), "password123");
        ApiHelper.SetBearer(_client, auth2.Token);

        var response = await _client.GetAsync($"/api/v1/simulations/{simId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

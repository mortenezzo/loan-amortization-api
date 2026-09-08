using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests.Health;

[Collection(nameof(ApiCollection))]
public class HealthTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_Returns200WithHealthyStatus()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<HealthResponseDto>(ApiHelper.JsonOpts);
        body!.Status.Should().Be("healthy");
        body.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

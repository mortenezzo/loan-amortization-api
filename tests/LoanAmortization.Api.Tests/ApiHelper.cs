using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests;

internal static class ApiHelper
{
    internal static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    internal static async Task<AuthResponse> RegisterAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password }, JsonOpts);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts))!;
    }

    internal static async Task<AuthResponse> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password }, JsonOpts);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts))!;
    }

    internal static void SetBearer(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

    internal static string UniqueEmail() => $"{Guid.NewGuid()}@test.com";
}

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LoanAmortization.Api.DTOs;

namespace LoanAmortization.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public class AuthTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_Returns201WithToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email = ApiHelper.UniqueEmail(), password = "password123" }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiHelper.JsonOpts);
        body!.Token.Should().NotBeNullOrEmpty();
        body.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = ApiHelper.UniqueEmail();
        await ApiHelper.RegisterAsync(_client, email, "password123");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password = "password123" }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("not-an-email", "password123")]
    [InlineData("valid@test.com", "short")]
    [InlineData("", "password123")]
    public async Task Register_InvalidInput_Returns400(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var email = ApiHelper.UniqueEmail();
        await ApiHelper.RegisterAsync(_client, email, "password123");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = "password123" }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(ApiHelper.JsonOpts);
        body!.Token.Should().NotBeNullOrEmpty();
        body.UserId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = ApiHelper.UniqueEmail();
        await ApiHelper.RegisterAsync(_client, email, "password123");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email, password = "wrongpassword" }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ApiHelper.UniqueEmail(), password = "password123" }, ApiHelper.JsonOpts);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

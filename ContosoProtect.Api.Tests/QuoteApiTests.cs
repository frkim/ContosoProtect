using System.Net;
using System.Net.Http.Json;
using ContosoProtect.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ContosoProtect.Api.Tests;

public class QuoteApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public QuoteApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task QuoteEndpoint_ValidRequest_ReturnsQuote()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 42,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = true,
            AccidentOption = false,
            SeniorityMonths = 36
        };

        // Act
        var response = await _client.PostAsJsonAsync("/quote", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        Assert.Equal(17m, quote.Premium); // 12 + 3 - 1 + 4 - 1
        Assert.NotEmpty(quote.Breakdown);
    }

    [Fact]
    public async Task QuoteEndpoint_InvalidAge_ReturnsBadRequest()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 17,
            Status = CustomerStatus.SalariedEmployee,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/quote", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ValidationErrorResponse>();
        Assert.NotNull(error);
        Assert.Equal("validation_error", error.Error);
        Assert.NotEmpty(error.Details);
        Assert.Contains(error.Details, d => d.Field == "age");
    }

    [Fact]
    public async Task QuoteEndpoint_HouseholdEmployer_ReturnsCorrectPremium()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 55,
            Status = CustomerStatus.HouseholdEmployer,
            FamilyOption = false,
            AccidentOption = false,
            SeniorityMonths = 12
        };

        // Act
        var response = await _client.PostAsJsonAsync("/quote", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        Assert.Equal(20m, quote.Premium); // 12 + 6 + 2
    }

    [Fact]
    public async Task AdminResetEndpoint_WithValidToken_ResetsDatabase()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/reset");
        request.Headers.Add("X-Admin-Token", "demo-reset");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("reset successfully", content);
    }

    [Fact]
    public async Task AdminResetEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.PostAsync("/admin/reset", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminResetEndpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/reset");
        request.Headers.Add("X-Admin-Token", "wrong-token");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QuoteEndpoint_ComplexScenario_CalculatesCorrectly()
    {
        // Arrange
        var request = new QuoteRequest
        {
            Age = 70,
            Status = CustomerStatus.HouseholdEmployer,
            FamilyOption = true,
            AccidentOption = true,
            SeniorityMonths = 30
        };

        // Act
        var response = await _client.PostAsJsonAsync("/quote", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        // 12 (base) + 9 (age 66-75) + 2 (employer) + 4 (family) + 3 (accident) - 1 (seniority) = 29
        Assert.Equal(29m, quote.Premium);
        Assert.Contains(quote.Breakdown, b => b.Code == "BASE");
        Assert.Contains(quote.Breakdown, b => b.Code == "AGE_66_75");
        Assert.Contains(quote.Breakdown, b => b.Code == "STATUS_EMPLOYER");
        Assert.Contains(quote.Breakdown, b => b.Code == "FAMILY_OPTION");
        Assert.Contains(quote.Breakdown, b => b.Code == "ACCIDENT_OPTION");
        Assert.Contains(quote.Breakdown, b => b.Code == "SENIORITY_DISCOUNT");
    }
}

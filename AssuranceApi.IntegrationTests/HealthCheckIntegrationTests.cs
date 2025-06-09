using FluentAssertions;
using System.Net;
using Xunit;

namespace AssuranceApi.IntegrationTests;

public class HealthCheckIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public HealthCheckIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthCheck_Returns200Ok()
    {
        // Arrange - Health check is public, no auth needed
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetHealthCheck_ReturnsHealthyResponse()
    {
        // Arrange - Health check is public, no auth needed
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("Healthy");
    }
} 
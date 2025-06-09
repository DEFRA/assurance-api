using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using AssuranceApi.Profession.Models;

namespace AssuranceApi.IntegrationTests;

public class AuthorizationIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public AuthorizationIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/professions")] // GET endpoints should be public
    [InlineData("/serviceStandards")]
    [InlineData("/projects")]
    [InlineData("/health")]
    public async Task PublicEndpoints_AllowUnauthenticatedAccess(string endpoint)
    {
        // Arrange - Clear database and create client WITHOUT authentication
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("POST", "/professions")] // Protected admin endpoints
    [InlineData("DELETE", "/professions/test-id")]
    [InlineData("POST", "/professions/deleteAll")]
    [InlineData("POST", "/serviceStandards/seed")]
    [InlineData("DELETE", "/serviceStandards/test-id")]
    [InlineData("POST", "/projects")]
    public async Task ProtectedEndpoints_RequireAuthentication(string method, string endpoint)
    {
        // Arrange - Clear database and create client WITHOUT authentication  
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        HttpResponseMessage response;
        if (method == "POST")
        {
            var dummyData = new ProfessionModel 
            { 
                Id = "test", 
                Name = "Test", 
                Description = "Test" 
            };
            response = await client.PostAsJsonAsync(endpoint, dummyData);
        }
        else if (method == "DELETE")
        {
            response = await client.DeleteAsync(endpoint);
        }
        else
        {
            response = await client.GetAsync(endpoint);
        }

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AuthenticatedClient_CanAccessProtectedEndpoints()
    {
        // Arrange - Clear database and create client WITH authentication
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();

        var profession = new ProfessionModel
        {
            Id = "auth-test-profession",
            Name = "Auth Test Profession", 
            Description = "Testing authenticated access"
        };

        // Act
        var response = await client.PostAsJsonAsync("/professions", profession);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AuthenticatedClient_CanDeleteProfession()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();
        
        // Create a profession first
        var profession = new ProfessionModel
        {
            Id = "delete-test-profession",
            Name = "Delete Test Profession",
            Description = "Testing authenticated deletion"
        };
        
        await client.PostAsJsonAsync("/professions", profession);

        // Act
        var response = await client.DeleteAsync("/professions/delete-test-profession");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnauthenticatedClient_CannotAccessAdminEndpoints()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync("/professions/deleteAll", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
} 
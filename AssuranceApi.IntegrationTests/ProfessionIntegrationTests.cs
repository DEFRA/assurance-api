using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssuranceApi.Profession.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AssuranceApi.IntegrationTests;

public class ProfessionIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ProfessionIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProfessions_ReturnsEmptyList_WhenNoProfessionsExist()
    {
        // Arrange - Clear database for clean test
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/api/v1.0/professions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var professions = JsonSerializer.Deserialize<List<ProfessionModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        professions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProfession_ReturnsCreated_WhenValidProfessionProvided()
    {
        // Arrange - Clear database and use authenticated client
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var profession = new ProfessionModel
        {
            Id = "test-profession",
            Name = "Test Profession",
            Description = "A test profession for integration testing",
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/professions", profession);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1.0/professions/test-profession");
    }

    [Fact]
    public async Task GetProfessionById_ReturnsNotFound_WhenProfessionDoesNotExist()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/api/v1.0/professions/non-existent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProfessionById_ReturnsProfession_WhenProfessionExists()
    {
        // Arrange - Clear database and create profession
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var profession = new ProfessionModel
        {
            Id = "test-get-profession",
            Name = "Test Get Profession",
            Description = "A profession for testing GET by ID",
        };

        // Create the profession first
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/professions", profession);

        // Act - Use public client to test read access
        var response = await publicClient.GetAsync("/api/v1.0/professions/test-get-profession");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedProfession = JsonSerializer.Deserialize<ProfessionModel>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedProfession.Should().NotBeNull();
        returnedProfession!.Id.Should().Be("test-get-profession");
        returnedProfession.Name.Should().Be("Test Get Profession");
    }

    [Fact]
    public async Task DeleteProfession_ReturnsOk_WhenProfessionExists()
    {
        // Arrange - Clear database and create profession
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var profession = new ProfessionModel
        {
            Id = "delete-test-profession",
            Name = "Delete Test Profession",
            Description = "A profession for testing deletion",
        };

        // Create the profession first
        await client.PostAsJsonAsync("/api/v1.0/professions", profession);

        // Act
        var response = await client.DeleteAsync("/api/v1.0/professions/delete-test-profession");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteAllProfessions_ReturnsOk_WhenAuthenticated()
    {
        // Arrange - Clear database and create some professions
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var profession1 = new ProfessionModel
        {
            Id = "prof1",
            Name = "Profession 1",
            Description = "Test 1",
        };
        var profession2 = new ProfessionModel
        {
            Id = "prof2",
            Name = "Profession 2",
            Description = "Test 2",
        };

        await client.PostAsJsonAsync("/api/v1.0/professions", profession1);
        await client.PostAsJsonAsync("/api/v1.0/professions", profession2);

        // Act
        var response = await client.PostAsync("/api/v1.0/professions/deleteAll", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify all professions are deleted
        var publicClient = _factory.CreateUnauthenticatedClient();
        var getResponse = await publicClient.GetAsync("/api/v1.0/professions");
        var content = await getResponse.Content.ReadAsStringAsync();
        var professions = JsonSerializer.Deserialize<List<ProfessionModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        professions.Should().BeEmpty();
    }
}

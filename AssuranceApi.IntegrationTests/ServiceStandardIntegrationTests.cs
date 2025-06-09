using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using AssuranceApi.ServiceStandard.Models;

namespace AssuranceApi.IntegrationTests;

public class ServiceStandardIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ServiceStandardIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetServiceStandards_ReturnsEmptyList_WhenNoStandardsExist()
    {
        // Arrange - Clear database for clean test
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/serviceStandards");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var standards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        standards.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedServiceStandards_ReturnsCreated_WhenValidStandardsProvided()
    {
        // Arrange - Clear database and use authenticated client
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel
            {
                Id = "1",
                Number = 1,
                Name = "Understand users and their needs",
                Description = "Understand user needs and the problem you're trying to solve.",
                IsActive = true
            },
            new ServiceStandardModel
            {
                Id = "2", 
                Number = 2,
                Name = "Solve a whole problem for users",
                Description = "Work towards creating a service that solves a whole problem for users.",
                IsActive = true
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/serviceStandards/seed", standards);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SeedServiceStandards_RequiresAuthentication()
    {
        // Arrange - Clear database and use unauthenticated client
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel
            {
                Id = "1",
                Number = 1,
                Name = "Test Standard",
                Description = "Test Description",
                IsActive = true
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/serviceStandards/seed", standards);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServiceStandards_ReturnsSeededStandards()
    {
        // Arrange - Clear database and seed some standards
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel
            {
                Id = "test-standard-1",
                Number = 1,
                Name = "Test Standard 1",
                Description = "First test standard",
                IsActive = true
            },
            new ServiceStandardModel
            {
                Id = "test-standard-2",
                Number = 2,
                Name = "Test Standard 2", 
                Description = "Second test standard",
                IsActive = true
            }
        };

        // Seed the standards
        await authenticatedClient.PostAsJsonAsync("/serviceStandards/seed", standards);

        // Act - Use public client to test read access
        var response = await publicClient.GetAsync("/serviceStandards");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedStandards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        returnedStandards.Should().NotBeNull();
        returnedStandards!.Should().HaveCount(2);
        returnedStandards.Should().Contain(s => s.Id == "test-standard-1");
        returnedStandards.Should().Contain(s => s.Id == "test-standard-2");
    }

    [Fact]
    public async Task DeleteServiceStandard_ReturnsOk_WhenStandardExists()
    {
        // Arrange - Clear database and seed a standard
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel
            {
                Id = "delete-test-standard",
                Number = 99,
                Name = "Delete Test Standard",
                Description = "A standard for testing deletion",
                IsActive = true
            }
        };

        // Seed the standard first
        await client.PostAsJsonAsync("/serviceStandards/seed", standards);

        // Act
        var response = await client.DeleteAsync("/serviceStandards/delete-test-standard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteServiceStandard_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.DeleteAsync("/serviceStandards/test-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteAllServiceStandards_ReturnsOk_WhenAuthenticated()
    {
        // Arrange - Clear database and seed some standards
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel { Id = "std1", Number = 1, Name = "Standard 1", Description = "Test 1", IsActive = true },
            new ServiceStandardModel { Id = "std2", Number = 2, Name = "Standard 2", Description = "Test 2", IsActive = true }
        };
        
        await client.PostAsJsonAsync("/serviceStandards/seed", standards);

        // Act
        var response = await client.PostAsync("/serviceStandards/deleteAll", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Verify all standards are deleted
        var publicClient = _factory.CreateUnauthenticatedClient();
        var getResponse = await publicClient.GetAsync("/serviceStandards");
        var content = await getResponse.Content.ReadAsStringAsync();
        var remainingStandards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        remainingStandards.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllServiceStandards_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync("/serviceStandards/deleteAll", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RestoreServiceStandard_ReturnsOk_WhenStandardExists()
    {
        // Arrange - Clear database, seed and delete a standard
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();
        
        var standards = new List<ServiceStandardModel>
        {
            new ServiceStandardModel
            {
                Id = "restore-test-standard",
                Number = 88,
                Name = "Restore Test Standard",
                Description = "A standard for testing restoration",
                IsActive = true
            }
        };

        // Seed and then delete the standard
        await client.PostAsJsonAsync("/serviceStandards/seed", standards);
        await client.DeleteAsync("/serviceStandards/restore-test-standard");

        // Act - Restore the standard
        var response = await client.PostAsync("/serviceStandards/restore-test-standard/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RestoreServiceStandard_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync("/serviceStandards/test-id/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
} 
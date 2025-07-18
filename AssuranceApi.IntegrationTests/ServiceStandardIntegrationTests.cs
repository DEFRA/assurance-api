using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssuranceApi.ServiceStandard.Models;
using FluentAssertions;

namespace AssuranceApi.IntegrationTests;

public class ServiceStandardIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ServiceStandardIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateServiceStandard_ReturnsCreated_AndAValidHistoryIsCreated_WhenAValidModelIsPassed()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();

        var newModel = new ServiceStandardModel
        {
            Id = "1",
            Number = 1,
            Name = "Understand users and their needs",
            Description = "Understand user needs and the problem you're trying to solve.",
            Guidance = "Guidance for Service Standard 1",
            IsActive = true,
        };

        // Act
        var response = await authenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/", newModel);
        var historyResponse = await authenticatedClient.GetAsync($"/api/v1.0/serviceStandards/{newModel.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedHistory = JsonSerializer.Deserialize<List<ServiceStandardHistory>>(
            await historyResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedHistory.Should().NotBeNull();
        returnedHistory.Should().HaveCount(1);
        returnedHistory[0].Changes.Name.To.Should().Be(newModel.Name);
        returnedHistory[0].Changes.Description.To.Should().Be(newModel.Description);
        returnedHistory[0].Changes.Guidance.To.Should().Be(newModel.Guidance);
    }

    [Fact]
    public async Task CreateServiceStandard_ReturnsConflict_WhenADuplicateModelIsPassed()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();

        var newModel = new ServiceStandardModel
        {
            Id = "1",
            Number = 1,
            Name = "Understand users and their needs",
            Description = "Understand user needs and the problem you're trying to solve.",
            Guidance = "Guidance for Service Standard 1",
            IsActive = true,
        };

        // Act
        var responseCreated = await authenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/", newModel);
        var responseConflict = await authenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/", newModel);

        // Assert
        responseCreated.StatusCode.Should().Be(HttpStatusCode.Created);
        responseConflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateServiceStandard_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var unauthenticatedClient = _factory.CreateUnauthenticatedClient();

        var newModel = new ServiceStandardModel
        {
            Id = "1",
            Number = 1,
            Name = "Understand users and their needs",
            Description = "Understand user needs and the problem you're trying to solve.",
            IsActive = true,
        };

        // Act
        var response = await unauthenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/", newModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateServiceStandard_ReturnsOk_AndHistoryIsUpdated_WhenValidModelIsPassed()
    {
        // Arrange - Clear database and seed a standard
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();

        var originalModel = new ServiceStandardModel
        {
            Id = "update-test-standard",
            Number = 10,
            Name = "Original Name",
            Description = "Original Description",
            Guidance = "Original Guidance",
            IsActive = true,
        };

        await client.PostAsJsonAsync("/api/v1.0/serviceStandards/", originalModel);

        var updatedModel = new ServiceStandardModel
        {
            Id = "update-test-standard",
            Number = 10,
            Name = "Updated Name",
            Description = "Updated Description",
            Guidance = "Updated Guidance",
            IsActive = false,
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1.0/serviceStandards/{updatedModel.Id}", updatedModel);
        var historyResponse = await client.GetAsync($"/api/v1.0/serviceStandards/{updatedModel.Id}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedHistory = JsonSerializer.Deserialize<List<ServiceStandardHistory>>(
            await historyResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedHistory.Should().NotBeNull();
        returnedHistory.Should().HaveCount(2);
        returnedHistory[0].Changes.Name.To.Should().Be(updatedModel.Name);
        returnedHistory[0].Changes.Description.To.Should().Be(updatedModel.Description);
        returnedHistory[0].Changes.Guidance.To.Should().Be(updatedModel.Guidance);
        returnedHistory[0].Changes.IsActive.To.Should().Be(updatedModel.IsActive.ToString());
    }

    [Fact]
    public async Task UpdateServiceStandard_ReturnsNotFound_WhenStandardDoesNotExist()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();

        var updatedModel = new ServiceStandardModel
        {
            Id = "nonexistent-standard",
            Number = 19,
            Name = "Does Not Exist",
            Description = "No Description",
            Guidance = "No Guidance",
            IsActive = true,
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1.0/serviceStandards/{updatedModel.Id}", updatedModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateServiceStandard_RequiresAuthentication()
    {
        // Arrange - Clear database and seed a standard
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var unauthenticatedClient = _factory.CreateUnauthenticatedClient();

        var originalModel = new ServiceStandardModel
        {
            Id = "update-auth-test-standard",
            Number = 19,
            Name = "Auth Test Name",
            Description = "Auth Test Description",
            Guidance = "Auth Test Guidance",
            IsActive = true,
        };

        await authenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/", originalModel);

        var updatedModel = new ServiceStandardModel
        {
            Id = "update-auth-test-standard",
            Number = 20,
            Name = "Updated Auth Name",
            Description = "Updated Auth Description",
            Guidance = "Updated Auth Guidance",
            IsActive = false,
        };

        // Act
        var response = await unauthenticatedClient.PutAsJsonAsync($"/api/v1.0/serviceStandards/{updatedModel.Id}", updatedModel);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetServiceStandards_ReturnsEmptyList_WhenNoStandardsExist()
    {
        // Arrange - Clear database for clean test
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/api/v1.0/serviceStandards");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var standards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        standards.Should().BeEmpty();
    }

    [Fact]
    public async Task SeedServiceStandards_ReturnsCreated_WhenValidStandardsProvided()
    {
        // Arrange - Clear database and use authenticated unauthenticatedClient
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
                Guidance = "Guidance for Service Standard 1",
                IsActive = true,
            },
            new ServiceStandardModel
            {
                Id = "2",
                Number = 2,
                Name = "Solve a whole problem for users",
                Description =
                    "Work towards creating a service that solves a whole problem for users.",
                Guidance = "Guidance for Service Standard 1",
                IsActive = true,
            },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SeedServiceStandards_RequiresAuthentication()
    {
        // Arrange - Clear database and use unauthenticated unauthenticatedClient
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
                IsActive = true,
            },
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);

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
                Guidance = "Guidance for Service Standard 1",
                IsActive = true,
            },
            new ServiceStandardModel
            {
                Id = "test-standard-2",
                Number = 2,
                Name = "Test Standard 2",
                Description = "Second test standard",
                Guidance = "Guidance for Service Standard 2",
                IsActive = true,
            },
        };

        // Seed the standards
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);

        // Act - Use public unauthenticatedClient to test read access
        var response = await publicClient.GetAsync("/api/v1.0/serviceStandards");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedStandards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

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
                Guidance = "Guidance for Service Standard delete",
                IsActive = true,
            },
        };

        // Seed the standard first
        await client.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);

        // Act
        var response = await client.DeleteAsync("/api/v1.0/serviceStandards/delete-test-standard");

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
        var response = await client.DeleteAsync("/api/v1.0/serviceStandards/test-id");

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
            new ServiceStandardModel
            {
                Id = "std1",
                Number = 1,
                Name = "Standard 1",
                Description = "Test 1",
                IsActive = true,
            },
            new ServiceStandardModel
            {
                Id = "std2",
                Number = 2,
                Name = "Standard 2",
                Description = "Test 2",
                IsActive = true,
            },
        };

        await client.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);

        // Act
        var response = await client.PostAsync("/api/v1.0/serviceStandards/deleteAll", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify all standards are deleted
        var publicClient = _factory.CreateUnauthenticatedClient();
        var getResponse = await publicClient.GetAsync("/api/v1.0/serviceStandards");
        var content = await getResponse.Content.ReadAsStringAsync();
        var remainingStandards = JsonSerializer.Deserialize<List<ServiceStandardModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        remainingStandards.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllServiceStandards_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.PostAsync("/api/v1.0/serviceStandards/deleteAll", null);

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
                Guidance = "Guidance for Service Standard Restore",
                IsActive = true,
            },
        };

        // Seed and then delete the standard
        await client.PostAsJsonAsync("/api/v1.0/serviceStandards/seed", standards);
        await client.DeleteAsync("/api/v1.0/serviceStandards/restore-test-standard");

        // Act - Restore the standard
        var response = await client.PostAsync(
            "/api/v1.0/serviceStandards/restore-test-standard/restore",
            null
        );

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
        var response = await client.PostAsync("/api/v1.0/serviceStandards/test-id/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssuranceApi.Insights.Models;
using AssuranceApi.Project.Models;
using AssuranceApi.ServiceStandard.Models;
using FluentAssertions;
using MongoDB.Bson;

namespace AssuranceApi.IntegrationTests;

public class InsightsIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InsightsIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    #region GetPrioritisation Tests

    [Fact]
    public async Task GetPrioritisation_ReturnsOk_WhenNoProjectsExist()
    {
        // Arrange - Clear database for clean test
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().BeEmpty();
        result.DeliveriesWithWorseningStandards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsDeliveriesNeedingUpdates_WhenProjectHasNoRecentStandardUpdates()
    {
        // Arrange - Clear database and create a project with no standard updates
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Stale Project",
            Commentary = "A project with no recent standard updates",
            Status = "GREEN",
            Phase = "Alpha",
            Tags = new List<string> { "test" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        // Create the project
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act - Get prioritisation data (project has no standard updates, so will appear as stale)
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().HaveCount(1);
        result.DeliveriesNeedingStandardUpdates[0].Id.Should().Be(projectId);
        result.DeliveriesNeedingStandardUpdates[0].Name.Should().Be("Stale Project");
    }

    [Fact]
    public async Task GetPrioritisation_UsesDefaultThresholds_WhenNoQueryParamsProvided()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrioritisation_UsesCustomThresholds_WhenQueryParamsProvided()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation?standardThreshold=7&worseningDays=30");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsEmptyWorseningStandards_WhenNoWorseningChanges()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Stable Project",
            Commentary = "A project with no worsening standards",
            Status = "GREEN",
            Phase = "Beta",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesWithWorseningStandards.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPrioritisation_ResponseContainsExpectedFields()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Test Fields Project",
            Commentary = "Test project for field validation",
            Status = "AMBER",
            Phase = "Discovery",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().NotBeEmpty();

        var delivery = result.DeliveriesNeedingStandardUpdates[0];
        delivery.Id.Should().NotBeNullOrEmpty();
        delivery.Name.Should().NotBeNullOrEmpty();
        delivery.Status.Should().NotBeNullOrEmpty();
        delivery.DaysSinceStandardUpdate.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPrioritisation_SortsByOldestFirst()
    {
        // Arrange - Create multiple projects
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        // Create first project
        var project1Id = ObjectId.GenerateNewId().ToString();
        var project1 = new ProjectModel
        {
            Id = project1Id,
            Name = "Project Alpha",
            Commentary = "First project",
            Status = "GREEN",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project1);

        // Create second project
        var project2Id = ObjectId.GenerateNewId().ToString();
        var project2 = new ProjectModel
        {
            Id = project2Id,
            Name = "Project Beta",
            Commentary = "Second project",
            Status = "AMBER",
            Phase = "Beta",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project2);

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().HaveCount(2);

        // All should have same daysSinceStandardUpdate since none have updates
        // The sort is by oldest first (most days since update)
        var days = result.DeliveriesNeedingStandardUpdates.Select(d => d.DaysSinceStandardUpdate).ToList();
        days.Should().BeInDescendingOrder();
    }

    #endregion
}


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
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act - Get prioritisation data (project has no standard updates, so will appear as stale)
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        // Check our specific project is in the results (not exact count to avoid parallel test issues)
        result!.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == projectId);
        result.DeliveriesNeedingStandardUpdates.First(d => d.Id == projectId).Name.Should().Be("Stale Project");
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
            Phase = "Private Beta",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        // The project has no standard history, so no worsening standards related to it
        result!.DeliveriesWithWorseningStandards.Should().NotContain(d => d.Id == projectId);
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

        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == projectId);

        var delivery = result.DeliveriesNeedingStandardUpdates.First(d => d.Id == projectId);
        delivery.Id.Should().NotBeNullOrEmpty();
        delivery.Name.Should().NotBeNullOrEmpty();
        delivery.Status.Should().NotBeNullOrEmpty();
        delivery.DaysSinceStandardUpdate.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsResultsSortedByOldestFirst()
    {
        // Arrange - Create a project (we just need at least one for this test)
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Sort Test Project",
            Commentary = "Project for sort order testing",
            Status = "GREEN",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().NotBeEmpty();

        // Verify the results are sorted by oldest first (descending by daysSinceStandardUpdate)
        // When there are multiple results, they should be in descending order
        if (result.DeliveriesNeedingStandardUpdates.Count > 1)
        {
            var days = result.DeliveriesNeedingStandardUpdates
                .Select(d => d.DaysSinceStandardUpdate)
                .ToList();
            days.Should().BeInDescendingOrder();
        }

        // Verify our created project is in the results
        result.DeliveriesNeedingStandardUpdates
            .Should().Contain(d => d.Id == projectId);
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsIntMaxValue_WhenProjectHasNoStandardUpdates()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Never Updated Project",
            Commentary = "Project with no standard history",
            Status = "GREEN",
            Phase = "Discovery",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        var delivery = result!.DeliveriesNeedingStandardUpdates.FirstOrDefault(d => d.Id == projectId);
        delivery.Should().NotBeNull("Project should be in results after creation");
        delivery!.DaysSinceStandardUpdate.Should().Be(int.MaxValue);
        delivery.LastServiceStandardUpdate.Should().BeNull();
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsProjectStatus_FromProjectModel()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Amber Status Project",
            Commentary = "Project with AMBER status",
            Status = "AMBER",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        var delivery = result!.DeliveriesNeedingStandardUpdates.FirstOrDefault(d => d.Id == projectId);
        delivery.Should().NotBeNull("Project should be in results after creation");
        delivery!.Status.Should().Be("AMBER");
    }

    [Fact]
    public async Task GetPrioritisation_ReturnsRedStatus_FromProjectModel()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Red Status Project",
            Commentary = "Project with RED status",
            Status = "RED",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        var delivery = result!.DeliveriesNeedingStandardUpdates.FirstOrDefault(d => d.Id == projectId);
        delivery.Should().NotBeNull("Project should be in results after creation");
        delivery!.Status.Should().Be("RED");
    }

    [Fact]
    public async Task GetPrioritisation_WithHistoryDepthParameter_ReturnsOk()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act - Test with custom historyDepth parameter
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation?historyDepth=3");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPrioritisation_WithAllParameters_ReturnsOk()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act - Test with all custom parameters
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation?standardThreshold=7&worseningDays=21&historyDepth=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().NotBeNull();
        result.DeliveriesWithWorseningStandards.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPrioritisation_WithZeroThreshold_ReturnsAllProjectsWithNoUpdates()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Zero Threshold Test",
            Commentary = "Test with 0 day threshold",
            Status = "GREEN",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var createResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Project creation should succeed");

        // Act - With 0 threshold, project with no updates should still appear
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation?standardThreshold=0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        result!.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == projectId);
    }

    [Fact]
    public async Task GetPrioritisation_MultipleProjectsWithDifferentStatuses_ReturnsAllInResults()
    {
        // Arrange
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        // Create projects with different statuses
        var greenProjectId = ObjectId.GenerateNewId().ToString();
        var greenProject = new ProjectModel
        {
            Id = greenProjectId,
            Name = "Green Project",
            Status = "GREEN",
            Commentary = "Green status",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var greenResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", greenProject);
        greenResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Green project creation should succeed");

        var amberProjectId = ObjectId.GenerateNewId().ToString();
        var amberProject = new ProjectModel
        {
            Id = amberProjectId,
            Name = "Amber Project",
            Status = "AMBER",
            Commentary = "Amber status",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var amberResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", amberProject);
        amberResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Amber project creation should succeed");

        var redProjectId = ObjectId.GenerateNewId().ToString();
        var redProject = new ProjectModel
        {
            Id = redProjectId,
            Name = "Red Project",
            Status = "RED",
            Commentary = "Red status",
            Phase = "Alpha",
            Tags = new List<string>(),
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var redResponse = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", redProject);
        redResponse.StatusCode.Should().Be(HttpStatusCode.Created, "Red project creation should succeed");

        // Act
        var response = await publicClient.GetAsync("/api/v1.0/insights/prioritisation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PrioritisationResponse>(content, _jsonOptions);

        result.Should().NotBeNull();
        // Check that our specific projects appear by ID (more reliable than status matching)
        result!.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == greenProjectId);
        result.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == amberProjectId);
        result.DeliveriesNeedingStandardUpdates.Should().Contain(d => d.Id == redProjectId);
    }

    [Fact]
    public async Task GetPrioritisation_LargeWorseningDays_ReturnsOk()
    {
        // Arrange - Test with a large worseningDays value
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act
        var response = await client.GetAsync("/api/v1.0/insights/prioritisation?worseningDays=365");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}


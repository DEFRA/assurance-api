using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssuranceApi.Project.Models;
using FluentAssertions;
using MongoDB.Bson;

namespace AssuranceApi.IntegrationTests;

public class ProjectIntegrationTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ProjectIntegrationTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_ReturnsEmptyList_WhenNoProjectsExist()
    {
        // Arrange - Clear database for clean test
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act
        var response = await client.GetAsync("/api/v1.0/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var projects = JsonSerializer.Deserialize<List<ProjectModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );
        projects.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateProject_ReturnsCreated_WhenValidProjectProvided()
    {
        // Arrange - Clear database and use authenticated client
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var project = new ProjectModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Test Project",
            Commentary = "A test project for integration testing",
            Status = "GREEN",
            Phase = "Discovery",
            Tags = new List<string> { "test", "integration" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/projects", project);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProject_RequiresAuthentication()
    {
        // Arrange - Clear database and use unauthenticated client
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        var project = new ProjectModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Test Project",
            Commentary = "Test Commentary",
            Status = "GREEN",
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/projects", project);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectById_ReturnsNotFound_WhenProjectDoesNotExist()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient(); // Public endpoint

        // Act - Use a valid ObjectId format
        var nonExistentId = ObjectId.GenerateNewId().ToString();
        var response = await client.GetAsync($"/api/v1.0/projects/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProject_WhenProjectExists()
    {
        // Arrange - Clear database and create project
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Test Get Project",
            Commentary = "A project for testing GET by ID",
            Status = "AMBER",
            Phase = "Alpha",
            Tags = new List<string> { "test" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the project first
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act - Use public client to test read access
        var response = await publicClient.GetAsync($"/api/v1.0/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedProject = JsonSerializer.Deserialize<ProjectModel>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedProject.Should().NotBeNull();
        returnedProject!.Id.Should().Be(projectId);
        returnedProject.Name.Should().Be("Test Get Project");
        returnedProject.Status.Should().Be("AMBER");
        returnedProject.Phase.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetProjects_ReturnsProjects_WhenProjectsExist()
    {
        // Arrange - Clear database and create projects
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var project1Id = ObjectId.GenerateNewId().ToString();
        var project2Id = ObjectId.GenerateNewId().ToString();

        var project1 = new ProjectModel
        {
            Id = project1Id,
            Name = "Project One",
            Commentary = "First test project",
            Status = "GREEN",
            Phase = "Live",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        var project2 = new ProjectModel
        {
            Id = project2Id,
            Name = "Project Two",
            Commentary = "Second test project",
            Status = "RED",
            Phase = "Alpha",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the projects
        var response1 = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        var response2 = await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Use public client to test read access
        var response = await publicClient.GetAsync("/api/v1.0/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedProjects = JsonSerializer.Deserialize<List<ProjectModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedProjects.Should().NotBeNull();
        returnedProjects!.Should().HaveCount(2);
        returnedProjects.Should().Contain(p => p.Id == project1Id);
        returnedProjects.Should().Contain(p => p.Id == project2Id);
    }

    [Fact]
    public async Task GetAllProjectsWithDateRange_ReturnsOkResult_WithListOfProjectsInRange_WhenStartAndEndDatesAreSpecified()
    {
        // Arrange - Clear database and create projects
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var dateRangeProjects = GetDateRangeProjects(5);

        foreach (var project in dateRangeProjects.Values)
        {
            await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);
        }

        var startDate = "2024-04-22T00:00:00Z";
        var endDate = "2024-04-23T00:00:00Z";

        var response = await publicClient.GetAsync($"/api/v1.0/projects?start_date={startDate}&end_date={endDate}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var returnedProjects = JsonSerializer.Deserialize<List<ProjectModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        returnedProjects.Should().NotBeNull();
        returnedProjects.Should().HaveCount(1);
    }

    private static Dictionary<string, ProjectModel> GetDateRangeProjects(int numberOfProjects)
    {
        var dateRangeProjects = new Dictionary<string, ProjectModel>(numberOfProjects);

        for (var projectNumber = 0; projectNumber < numberOfProjects; projectNumber++)
        {
            var projectId = ObjectId.GenerateNewId().ToString();

            var project = new ProjectModel
            {
                Id = projectId,
                Name = $"Project {projectNumber + 1}",
                Commentary = $"{projectNumber + 1} test project",
                Status = "RED",
                Phase = "Alpha",
                LastUpdated = new DateTime(2024, 04, 21 + projectNumber).ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            dateRangeProjects.Add(projectId, project);
        }

        return dateRangeProjects;
    }

    [Fact]
    public async Task GetProjectsWithTagFilter_ReturnsFilteredResults()
    {
        // Arrange - Clear database and create projects with different tags
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var project1Id = ObjectId.GenerateNewId().ToString();
        var project2Id = ObjectId.GenerateNewId().ToString();

        var project1 = new ProjectModel
        {
            Id = project1Id,
            Name = "Tagged Project 1",
            Commentary = "Project with API tag",
            Status = "GREEN",
            Phase = "Live",
            Tags = new List<string> { "api", "backend" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        var project2 = new ProjectModel
        {
            Id = project2Id,
            Name = "Tagged Project 2",
            Commentary = "Project with frontend tag",
            Status = "AMBER",
            Phase = "Beta",
            Tags = new List<string> { "frontend", "ui" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the projects
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project1);
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project2);

        // Act - Filter by API tag
        var response = await publicClient.GetAsync("/api/v1.0/projects?tag=api");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var filteredProjects = JsonSerializer.Deserialize<List<ProjectModel>>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        filteredProjects.Should().NotBeNull();
        filteredProjects!.Should().HaveCount(1);
        filteredProjects[0].Id.Should().Be(project1Id);
    }

    [Fact]
    public async Task UpdateProject_ReturnsOk_WhenValidUpdateProvided()
    {
        // Arrange - Clear database and create project
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var projectId = ObjectId.GenerateNewId().ToString();
        var originalProject = new ProjectModel
        {
            Id = projectId,
            Name = "Original Project Name",
            Commentary = "Original commentary",
            Status = "GREEN",
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the project first
        await client.PostAsJsonAsync("/api/v1.0/projects", originalProject);

        var updatedProject = new ProjectModel
        {
            Id = projectId,
            Name = "Updated Project Name",
            Commentary = "Updated commentary",
            Status = "AMBER",
            Phase = "Alpha",
            Tags = new List<string> { "updated" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1.0/projects/{projectId}", updatedProject);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the update by fetching the project
        var getResponse = await client.GetAsync($"/api/v1.0/projects/{projectId}");
        var content = await getResponse.Content.ReadAsStringAsync();
        var fetchedProject = JsonSerializer.Deserialize<ProjectModel>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        fetchedProject!.Name.Should().Be("Updated Project Name");
        fetchedProject.Status.Should().Be("AMBER");
        fetchedProject.Phase.Should().Be("Alpha");
    }

    [Fact]
    public async Task UpdateProject_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Test Project",
            Commentary = "Test Commentary",
            Status = "GREEN",
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1.0/projects/{projectId}", project);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProject_ReturnsOk_WhenProjectExists()
    {
        // Arrange - Clear database and create project
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient(); // Protected endpoint

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "Delete Test Project",
            Commentary = "A project for testing deletion",
            Status = "GREEN",
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the project first
        await client.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act
        var response = await client.DeleteAsync($"/api/v1.0/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProject_RequiresAuthentication()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateUnauthenticatedClient();

        // Act - Use valid ObjectId format
        var projectId = ObjectId.GenerateNewId().ToString();
        var response = await client.DeleteAsync($"/api/v1.0/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectHistory_ReturnsOk_WhenProjectExists()
    {
        // Arrange - Clear database and create project
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var projectId = ObjectId.GenerateNewId().ToString();
        var project = new ProjectModel
        {
            Id = projectId,
            Name = "History Test Project",
            Commentary = "A project for testing history",
            Status = "GREEN",
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the project first
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project);

        // Act - History endpoint is public
        var response = await publicClient.GetAsync($"/api/v1.0/projects/{projectId}/history");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        // History should be empty for a newly created project
    }

    [Fact]
    public async Task GetTagsSummary_ReturnsOk()
    {
        // Arrange - Clear database and create projects with tags
        await _factory.ClearDatabaseAsync();
        var authenticatedClient = _factory.CreateAuthenticatedClient();
        var publicClient = _factory.CreateUnauthenticatedClient();

        var project1Id = ObjectId.GenerateNewId().ToString();
        var project2Id = ObjectId.GenerateNewId().ToString();

        var project1 = new ProjectModel
        {
            Id = project1Id,
            Name = "Tags Project 1",
            Commentary = "Project with tags",
            Status = "GREEN",
            Phase = "Live",
            Tags = new List<string> { "api", "backend", "microservice" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        var project2 = new ProjectModel
        {
            Id = project2Id,
            Name = "Tags Project 2",
            Commentary = "Another project with tags",
            Status = "AMBER",
            Phase = "Beta",
            Tags = new List<string> { "frontend", "ui", "react" },
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Create the projects
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project1);
        await authenticatedClient.PostAsJsonAsync("/api/v1.0/projects", project2);

        // Act - Tags summary endpoint is public
        var response = await publicClient.GetAsync("/api/v1.0/projects/tags/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNull();
        // Should contain tag summary information
    }

    [Theory]
    [InlineData("INVALID_STATUS")] // Invalid status
    [InlineData("")] // Empty status
    public async Task CreateProject_ReturnsBadRequest_WithInvalidStatus(string status)
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();

        var project = new ProjectModel
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Name = "Invalid Status Project",
            Commentary = "Project with invalid status",
            Status = status,
            Phase = "Discovery",
            LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1.0/projects", project);

        // Assert
        response
            .StatusCode.Should()
            .BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateProject_WithValidProjectStatuses_ReturnsCreated()
    {
        // Arrange - Clear database
        await _factory.ClearDatabaseAsync();
        var client = _factory.CreateAuthenticatedClient();

        var validStatuses = new[] { "RED", "AMBER_RED", "AMBER", "GREEN_AMBER", "GREEN", "TBC" };

        // Act & Assert
        for (int i = 0; i < validStatuses.Length; i++)
        {
            var project = new ProjectModel
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Name = $"Status Test Project {i}",
                Commentary = $"Project with {validStatuses[i]} status",
                Status = validStatuses[i],
                Phase = "Discovery",
                LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };

            var response = await client.PostAsJsonAsync("/api/v1.0/projects", project);
            response
                .StatusCode.Should()
                .Be(HttpStatusCode.Created, $"Status {validStatuses[i]} should be valid");
        }
    }
}

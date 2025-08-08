using AssuranceApi.Data;
using AssuranceApi.Project.Models;
using FluentAssertions;
using NSubstitute;

namespace AssuranceApi.Test.Project.Services;

public class ProjectServiceTests
{
    private readonly IProjectPersistence _projectPersistence =
        Substitute.For<IProjectPersistence>();

    private ProjectService CreateService()
    {
        return new ProjectService(_projectPersistence);
    }

    [Fact]
    public async Task CreateProject_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var newProject = new ProjectModel
        {
            Id = "project-1",
            Name = "Test Project",
            Status = "GREEN",
            Commentary = "Test commentary",
            Tags = new List<string> { "web", "api" },
        };

        _projectPersistence.CreateAsync(newProject).Returns(true);

        // Act
        var result = await service.CreateProjectAsync(newProject);

        // Assert
        result.Should().BeTrue();
        await _projectPersistence.Received(1).CreateAsync(newProject);
    }

    [Fact]
    public async Task GetAllProjects_ShouldReturnFilteredByTag_WhenTagProvided()
    {
        // Arrange
        var service = CreateService();
        var tag = "web";
        var expectedProjects = new List<ProjectModel>
        {
            new()
            {
                Id = "1",
                Name = "Web Project",
                Tags = new List<string> { "web" },
            },
            new()
            {
                Id = "2",
                Name = "Another Web Project",
                Tags = new List<string> { "web", "api" },
            },
        };

        _projectPersistence.GetAllAsync(new ProjectQueryParameters(tag, string.Empty, string.Empty)).Returns(expectedProjects);

        // Act
        var result = await service.GetProjectsByTagAsync(tag);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedProjects);
        await _projectPersistence.Received(1).GetAllAsync(new ProjectQueryParameters(tag, string.Empty, string.Empty));
    }

    [Fact]
    public async Task GetAllProjects_ShouldReturnAll_WhenNoTagFilter()
    {
        // Arrange
        var service = CreateService();
        var expectedProjects = new List<ProjectModel>
        {
            new()
            {
                Id = "1",
                Name = "Project 1",
                Status = "GREEN",
            },
            new()
            {
                Id = "2",
                Name = "Project 2",
                Status = "AMBER",
            },
            new()
            {
                Id = "3",
                Name = "Project 3",
                Status = "RED",
            },
        };

        _projectPersistence.GetAllAsync(new ProjectQueryParameters(null, null, null)).Returns(expectedProjects);

        // Act
        var result = await service.GetAllProjectsAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedProjects);
        await _projectPersistence.Received(1).GetAllAsync(null);
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnProject_WhenExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-123";
        var expectedProject = new ProjectModel
        {
            Id = projectId,
            Name = "Test Project",
            Status = "GREEN",
            Commentary = "Test commentary",
        };

        _projectPersistence.GetByIdAsync(projectId).Returns(expectedProject);

        // Act
        var result = await service.GetProjectByIdAsync(projectId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(projectId);
        result.Name.Should().Be("Test Project");
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
    }

    [Fact]
    public async Task GetProjectById_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "nonexistent";

        _projectPersistence.GetByIdAsync(projectId).Returns((ProjectModel?)null);

        // Act
        var result = await service.GetProjectByIdAsync(projectId);

        // Assert
        result.Should().BeNull();
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-123";
        var updatedProject = new ProjectModel
        {
            Name = "Updated Project",
            Status = "AMBER",
            Commentary = "Updated commentary",
        };

        _projectPersistence.UpdateAsync(projectId, updatedProject).Returns(true);

        // Act
        var result = await service.UpdateProjectAsync(projectId, updatedProject);

        // Assert
        result.Should().BeTrue();
        await _projectPersistence.Received(1).UpdateAsync(projectId, updatedProject);
    }

    [Fact]
    public async Task UpdateProject_ShouldReturnFalse_WhenFailed()
    {
        // Arrange
        var service = CreateService();
        var projectId = "nonexistent";
        var updatedProject = new ProjectModel { Name = "Updated Project" };

        _projectPersistence.UpdateAsync(projectId, updatedProject).Returns(false);

        // Act
        var result = await service.UpdateProjectAsync(projectId, updatedProject);

        // Assert
        result.Should().BeFalse();
        await _projectPersistence.Received(1).UpdateAsync(projectId, updatedProject);
    }

    [Fact]
    public async Task DeleteProject_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-123";

        _projectPersistence.DeleteAsync(projectId).Returns(true);

        // Act
        var result = await service.DeleteProjectAsync(projectId);

        // Assert
        result.Should().BeTrue();
        await _projectPersistence.Received(1).DeleteAsync(projectId);
    }

    [Fact]
    public async Task BulkCreateProjects_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var projects = new List<ProjectModel>
        {
            new() { Id = "1", Name = "Project 1" },
            new() { Id = "2", Name = "Project 2" },
        };

        _projectPersistence.AddProjectsAsync(projects).Returns(true);

        // Act
        var result = await service.BulkCreateProjectsAsync(projects);

        // Assert
        result.Should().BeTrue();
        await _projectPersistence.Received(1).AddProjectsAsync(projects);
    }
}

// Service layer for project management
public class ProjectService
{
    private readonly IProjectPersistence _projectPersistence;

    public ProjectService(IProjectPersistence projectPersistence)
    {
        _projectPersistence = projectPersistence;
    }

    public async Task<bool> CreateProjectAsync(ProjectModel project)
    {
        return await _projectPersistence.CreateAsync(project);
    }

    public async Task<List<ProjectModel>> GetAllProjectsAsync()
    {
        return await _projectPersistence.GetAllAsync(new ProjectQueryParameters());
    }

    public async Task<List<ProjectModel>> GetProjectsByTagAsync(string tag)
    {
        return await _projectPersistence.GetAllAsync(new ProjectQueryParameters(tag, null, null));
    }

    public async Task<ProjectModel?> GetProjectByIdAsync(string id)
    {
        return await _projectPersistence.GetByIdAsync(id);
    }

    public async Task<bool> UpdateProjectAsync(string id, ProjectModel project)
    {
        return await _projectPersistence.UpdateAsync(id, project);
    }

    public async Task<bool> DeleteProjectAsync(string id)
    {
        return await _projectPersistence.DeleteAsync(id);
    }

    public async Task<bool> BulkCreateProjectsAsync(List<ProjectModel> projects)
    {
        return await _projectPersistence.AddProjectsAsync(projects);
    }

    public async Task<bool> SeedProjectsAsync(List<ProjectModel> projects)
    {
        return await _projectPersistence.SeedAsync(projects);
    }
}

using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentAssertions;
using NSubstitute;

namespace AssuranceApi.Test.Project.Services;

public class ProjectStandardsServiceTests
{
    private readonly IProjectStandardsPersistence _projectStandardsPersistence = Substitute.For<IProjectStandardsPersistence>();

    private ProjectStandardsService CreateService()
    {
        return new ProjectStandardsService(_projectStandardsPersistence);
    }

    [Fact]
    public async Task GetAssessment_ShouldReturnAssessment_WhenExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";
        var standardId = "standard-1";
        var professionId = "profession-1";
        
        var expectedAssessment = new ProjectStandards
        {
            ProjectId = projectId,
            StandardId = standardId,
            ProfessionId = professionId,
            Status = "GREEN",
            Commentary = "Meets all requirements"
        };

        _projectStandardsPersistence.GetAsync(projectId, standardId, professionId).Returns(expectedAssessment);

        // Act
        var result = await service.GetAssessmentAsync(projectId, standardId, professionId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("GREEN");
        result.Commentary.Should().Be("Meets all requirements");
        await _projectStandardsPersistence.Received(1).GetAsync(projectId, standardId, professionId);
    }

    [Fact]
    public async Task GetAssessment_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "nonexistent";
        var standardId = "nonexistent";
        var professionId = "nonexistent";

        _projectStandardsPersistence.GetAsync(projectId, standardId, professionId).Returns((ProjectStandards?)null);

        // Act
        var result = await service.GetAssessmentAsync(projectId, standardId, professionId);

        // Assert
        result.Should().BeNull();
        await _projectStandardsPersistence.Received(1).GetAsync(projectId, standardId, professionId);
    }

    [Fact]
    public async Task GetProjectStandardAssessments_ShouldReturnAssessments_WhenExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";
        var standardId = "standard-1";

        var expectedAssessments = new List<ProjectStandards>
        {
            new()
            {
                ProjectId = projectId,
                StandardId = standardId,
                ProfessionId = "profession-1",
                Status = "GREEN"
            },
            new()
            {
                ProjectId = projectId,
                StandardId = standardId,
                ProfessionId = "profession-2",
                Status = "AMBER"
            }
        };

        _projectStandardsPersistence.GetByProjectAndStandardAsync(projectId, standardId).Returns(expectedAssessments);

        // Act
        var result = await service.GetProjectStandardAssessmentsAsync(projectId, standardId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().BeEquivalentTo(expectedAssessments);
        await _projectStandardsPersistence.Received(1).GetByProjectAndStandardAsync(projectId, standardId);
    }

    [Fact]
    public async Task GetAllProjectAssessments_ShouldReturnAllAssessments_ForProject()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";

        var expectedAssessments = new List<ProjectStandards>
        {
            new()
            {
                ProjectId = projectId,
                StandardId = "standard-1",
                ProfessionId = "profession-1",
                Status = "GREEN"
            },
            new()
            {
                ProjectId = projectId,
                StandardId = "standard-2",
                ProfessionId = "profession-1",
                Status = "AMBER"
            },
            new()
            {
                ProjectId = projectId,
                StandardId = "standard-1",
                ProfessionId = "profession-2",
                Status = "RED"
            }
        };

        _projectStandardsPersistence.GetByProjectAsync(projectId).Returns(expectedAssessments);

        // Act
        var result = await service.GetAllProjectAssessmentsAsync(projectId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().BeEquivalentTo(expectedAssessments);
        await _projectStandardsPersistence.Received(1).GetByProjectAsync(projectId);
    }

    [Fact]
    public async Task UpsertAssessment_ShouldCallPersistence()
    {
        // Arrange
        var service = CreateService();
        var assessment = new ProjectStandards
        {
            ProjectId = "project-1",
            StandardId = "standard-1",
            ProfessionId = "profession-1",
            Status = "GREEN",
            Commentary = "Updated assessment"
        };

        // Act
        await service.UpsertAssessmentAsync(assessment);

        // Assert
        await _projectStandardsPersistence.Received(1).UpsertAsync(assessment);
    }

    [Fact]
    public async Task DeleteAssessment_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";
        var standardId = "standard-1";
        var professionId = "profession-1";

        _projectStandardsPersistence.DeleteAsync(projectId, standardId, professionId).Returns(true);

        // Act
        var result = await service.DeleteAssessmentAsync(projectId, standardId, professionId);

        // Assert
        result.Should().BeTrue();
        await _projectStandardsPersistence.Received(1).DeleteAsync(projectId, standardId, professionId);
    }

    [Fact]
    public async Task GetProjectOverview_ShouldCalculateStatusSummary()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";

        var assessments = new List<ProjectStandards>
        {
            new() { ProjectId = projectId, StandardId = "1", Status = "GREEN" },
            new() { ProjectId = projectId, StandardId = "2", Status = "GREEN" },
            new() { ProjectId = projectId, StandardId = "3", Status = "AMBER" },
            new() { ProjectId = projectId, StandardId = "4", Status = "RED" },
            new() { ProjectId = projectId, StandardId = "5", Status = "RED" }
        };

        _projectStandardsPersistence.GetByProjectAsync(projectId).Returns(assessments);

        // Act
        var result = await service.GetProjectOverviewAsync(projectId);

        // Assert
        result.TotalAssessments.Should().Be(5);
        result.GreenCount.Should().Be(2);
        result.AmberCount.Should().Be(1);
        result.RedCount.Should().Be(2);
        result.CompletionPercentage.Should().Be(100); // All assessed
    }
}

// Service layer for project standards assessment
public class ProjectStandardsService
{
    private readonly IProjectStandardsPersistence _projectStandardsPersistence;

    public ProjectStandardsService(IProjectStandardsPersistence projectStandardsPersistence)
    {
        _projectStandardsPersistence = projectStandardsPersistence;
    }

    public async Task<ProjectStandards?> GetAssessmentAsync(string projectId, string standardId, string professionId)
    {
        return await _projectStandardsPersistence.GetAsync(projectId, standardId, professionId);
    }

    public async Task<List<ProjectStandards>> GetProjectStandardAssessmentsAsync(string projectId, string standardId)
    {
        return await _projectStandardsPersistence.GetByProjectAndStandardAsync(projectId, standardId);
    }

    public async Task<List<ProjectStandards>> GetAllProjectAssessmentsAsync(string projectId)
    {
        return await _projectStandardsPersistence.GetByProjectAsync(projectId);
    }

    public async Task UpsertAssessmentAsync(ProjectStandards assessment)
    {
        await _projectStandardsPersistence.UpsertAsync(assessment);
    }

    public async Task<bool> DeleteAssessmentAsync(string projectId, string standardId, string professionId)
    {
        return await _projectStandardsPersistence.DeleteAsync(projectId, standardId, professionId);
    }

    public async Task<ProjectOverview> GetProjectOverviewAsync(string projectId)
    {
        var assessments = await _projectStandardsPersistence.GetByProjectAsync(projectId);
        
        return new ProjectOverview
        {
            ProjectId = projectId,
            TotalAssessments = assessments.Count,
            GreenCount = assessments.Count(a => a.Status == "GREEN"),
            AmberCount = assessments.Count(a => a.Status == "AMBER"),
            RedCount = assessments.Count(a => a.Status == "RED"),
            CompletionPercentage = assessments.Count > 0 ? 100 : 0 // Simplified - could be more complex
        };
    }
}

// DTO for project overview
public class ProjectOverview
{
    public string ProjectId { get; set; } = string.Empty;
    public int TotalAssessments { get; set; }
    public int GreenCount { get; set; }
    public int AmberCount { get; set; }
    public int RedCount { get; set; }
    public int CompletionPercentage { get; set; }
} 
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using FluentAssertions;
using NSubstitute;

namespace AssuranceApi.Test.Validation;

public class ValidationServiceTests
{
    private readonly IProjectPersistence _projectPersistence =
        Substitute.For<IProjectPersistence>();
    private readonly IServiceStandardPersistence _serviceStandardPersistence =
        Substitute.For<IServiceStandardPersistence>();
    private readonly IProjectStandardsPersistence _projectStandardsPersistence =
        Substitute.For<IProjectStandardsPersistence>();

    private ValidationService CreateService()
    {
        return new ValidationService(
            _projectPersistence,
            _serviceStandardPersistence,
            _projectStandardsPersistence
        );
    }

    [Fact]
    public async Task ValidateProjectExists_ShouldReturnTrue_WhenProjectExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";
        var project = new ProjectModel { Id = projectId, Name = "Test Project" };

        _projectPersistence.GetByIdAsync(projectId).Returns(project);

        // Act
        var result = await service.ValidateProjectExistsAsync(projectId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
    }

    [Fact]
    public async Task ValidateProjectExists_ShouldReturnFalse_WhenProjectNotExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "nonexistent";

        _projectPersistence.GetByIdAsync(projectId).Returns((ProjectModel?)null);

        // Act
        var result = await service.ValidateProjectExistsAsync(projectId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"Project with ID '{projectId}' not found");
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
    }

    [Fact]
    public async Task ValidateStandardExists_ShouldReturnTrue_WhenStandardExists()
    {
        // Arrange
        var service = CreateService();
        var standardId = "standard-1";
        var standard = new ServiceStandardModel
        {
            Id = standardId,
            Name = "Test Standard",
            IsActive = true,
        };

        _serviceStandardPersistence.GetActiveByIdAsync(standardId).Returns(standard);

        // Act
        var result = await service.ValidateStandardExistsAsync(standardId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
        await _serviceStandardPersistence.Received(1).GetActiveByIdAsync(standardId);
    }

    [Fact]
    public async Task ValidateStandardExists_ShouldReturnFalse_WhenStandardNotActive()
    {
        // Arrange
        var service = CreateService();
        var standardId = "inactive-standard";

        _serviceStandardPersistence
            .GetActiveByIdAsync(standardId)
            .Returns((ServiceStandardModel?)null);

        // Act
        var result = await service.ValidateStandardExistsAsync(standardId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be($"Active standard with ID '{standardId}' not found");
        await _serviceStandardPersistence.Received(1).GetActiveByIdAsync(standardId);
    }

    [Fact]
    public async Task ValidateAssessmentData_ShouldReturnTrue_WhenAllValid()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";
        var standardId = "standard-1";
        var professionId = "profession-1";

        var project = new ProjectModel { Id = projectId, Name = "Test Project" };
        var standard = new ServiceStandardModel
        {
            Id = standardId,
            Name = "Test Standard",
            IsActive = true,
        };

        _projectPersistence.GetByIdAsync(projectId).Returns(project);
        _serviceStandardPersistence.GetActiveByIdAsync(standardId).Returns(standard);

        // Act
        var result = await service.ValidateAssessmentDataAsync(projectId, standardId, professionId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNullOrEmpty();
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
        await _serviceStandardPersistence.Received(1).GetActiveByIdAsync(standardId);
    }

    [Fact]
    public async Task ValidateAssessmentData_ShouldReturnFalse_WhenProjectNotExists()
    {
        // Arrange
        var service = CreateService();
        var projectId = "nonexistent";
        var standardId = "standard-1";
        var professionId = "profession-1";

        _projectPersistence.GetByIdAsync(projectId).Returns((ProjectModel?)null);

        // Act
        var result = await service.ValidateAssessmentDataAsync(projectId, standardId, professionId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Project");
        await _projectPersistence.Received(1).GetByIdAsync(projectId);
        await _serviceStandardPersistence.DidNotReceive().GetActiveByIdAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetProjectCompletionStatus_ShouldReturnCorrectPercentage()
    {
        // Arrange
        var service = CreateService();
        var projectId = "project-1";

        var allStandards = new List<ServiceStandardModel>
        {
            new()
            {
                Id = "1",
                Name = "Standard 1",
                IsActive = true,
            },
            new()
            {
                Id = "2",
                Name = "Standard 2",
                IsActive = true,
            },
            new()
            {
                Id = "3",
                Name = "Standard 3",
                IsActive = true,
            },
            new()
            {
                Id = "4",
                Name = "Standard 4",
                IsActive = true,
            },
        };

        var assessments = new List<ProjectStandards>
        {
            new()
            {
                ProjectId = projectId,
                StandardId = "1",
                Status = "GREEN",
            },
            new()
            {
                ProjectId = projectId,
                StandardId = "2",
                Status = "AMBER",
            },
            // Standards 3 and 4 not assessed
        };

        _serviceStandardPersistence.GetAllActiveAsync().Returns(allStandards);
        _projectStandardsPersistence.GetByProjectAsync(projectId).Returns(assessments);

        // Act
        var result = await service.GetProjectCompletionStatusAsync(projectId);

        // Assert
        result.TotalStandards.Should().Be(4);
        result.AssessedStandards.Should().Be(2);
        result.CompletionPercentage.Should().Be(50);
        result.PendingStandards.Should().HaveCount(2);
        result.PendingStandards.Should().Contain(s => s.Id == "3");
        result.PendingStandards.Should().Contain(s => s.Id == "4");
    }

    [Fact]
    public async Task ValidateStatusTransition_ShouldAllowValidTransitions()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var validTransitions = new[]
        {
            (null, "GREEN"),
            ("GREEN", "AMBER"),
            ("GREEN", "RED"),
            ("AMBER", "GREEN"),
            ("AMBER", "RED"),
            ("RED", "GREEN"),
            ("RED", "AMBER"),
        };

        foreach (var (currentStatus, newStatus) in validTransitions)
        {
            var result = service.ValidateStatusTransition(currentStatus, newStatus);
            result
                .IsValid.Should()
                .BeTrue($"Transition from '{currentStatus}' to '{newStatus}' should be valid");
        }
    }

    [Fact]
    public async Task ValidateStatusTransition_ShouldRejectInvalidStatuses()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var invalidTransitions = new[]
        {
            ("GREEN", "INVALID"),
            ("AMBER", "PURPLE"),
            ("RED", "BLUE"),
            ("UNKNOWN", "GREEN"),
        };

        foreach (var (currentStatus, newStatus) in invalidTransitions)
        {
            var result = service.ValidateStatusTransition(currentStatus, newStatus);
            result
                .IsValid.Should()
                .BeFalse($"Transition from '{currentStatus}' to '{newStatus}' should be invalid");
        }
    }
}

// Validation service that coordinates multiple domain services
public class ValidationService
{
    private readonly IProjectPersistence _projectPersistence;
    private readonly IServiceStandardPersistence _serviceStandardPersistence;
    private readonly IProjectStandardsPersistence _projectStandardsPersistence;

    private readonly string[] ValidStatuses = { "GREEN", "AMBER", "RED" };

    public ValidationService(
        IProjectPersistence projectPersistence,
        IServiceStandardPersistence serviceStandardPersistence,
        IProjectStandardsPersistence projectStandardsPersistence
    )
    {
        _projectPersistence = projectPersistence;
        _serviceStandardPersistence = serviceStandardPersistence;
        _projectStandardsPersistence = projectStandardsPersistence;
    }

    public async Task<ValidationResult> ValidateProjectExistsAsync(string projectId)
    {
        var project = await _projectPersistence.GetByIdAsync(projectId);

        return project != null
            ? ValidationResult.Success()
            : ValidationResult.Failure($"Project with ID '{projectId}' not found");
    }

    public async Task<ValidationResult> ValidateStandardExistsAsync(string standardId)
    {
        var standard = await _serviceStandardPersistence.GetActiveByIdAsync(standardId);

        return standard != null
            ? ValidationResult.Success()
            : ValidationResult.Failure($"Active standard with ID '{standardId}' not found");
    }

    public async Task<ValidationResult> ValidateAssessmentDataAsync(
        string projectId,
        string standardId,
        string professionId
    )
    {
        var projectValidation = await ValidateProjectExistsAsync(projectId);
        if (!projectValidation.IsValid)
            return projectValidation;

        var standardValidation = await ValidateStandardExistsAsync(standardId);
        if (!standardValidation.IsValid)
            return standardValidation;

        if (string.IsNullOrWhiteSpace(professionId))
            return ValidationResult.Failure("Profession ID is required");

        return ValidationResult.Success();
    }

    public ValidationResult ValidateStatusTransition(string? currentStatus, string newStatus)
    {
        if (!ValidStatuses.Contains(newStatus))
            return ValidationResult.Failure(
                $"Invalid status: {newStatus}. Valid statuses are: {string.Join(", ", ValidStatuses)}"
            );

        // Check if currentStatus is valid when it's not null
        if (currentStatus != null && !ValidStatuses.Contains(currentStatus))
            return ValidationResult.Failure(
                $"Invalid current status: {currentStatus}. Valid statuses are: {string.Join(", ", ValidStatuses)}"
            );

        // For now, allow all transitions between valid statuses
        // Could add more complex business rules here
        return ValidationResult.Success();
    }

    public async Task<ProjectCompletionStatus> GetProjectCompletionStatusAsync(string projectId)
    {
        var allStandards = await _serviceStandardPersistence.GetAllActiveAsync();
        var assessments = await _projectStandardsPersistence.GetByProjectAsync(projectId);

        var assessedStandardIds = assessments.Select(a => a.StandardId).ToHashSet();
        var pendingStandards = allStandards
            .Where(s => !assessedStandardIds.Contains(s.Id))
            .ToList();

        var completionPercentage =
            allStandards.Count > 0 ? (assessments.Count * 100) / allStandards.Count : 0;

        return new ProjectCompletionStatus
        {
            ProjectId = projectId,
            TotalStandards = allStandards.Count,
            AssessedStandards = assessments.Count,
            CompletionPercentage = completionPercentage,
            PendingStandards = pendingStandards,
        };
    }
}

// DTOs for validation
public class ValidationResult
{
    public bool IsValid { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;

    private ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success() => new(true);

    public static ValidationResult Failure(string errorMessage) => new(false, errorMessage);
}

public class ProjectCompletionStatus
{
    public string ProjectId { get; set; } = string.Empty;
    public int TotalStandards { get; set; }
    public int AssessedStandards { get; set; }
    public int CompletionPercentage { get; set; }
    public List<ServiceStandardModel> PendingStandards { get; set; } = new();
}

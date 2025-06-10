using AssuranceApi.Profession.Services;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.ServiceStandard.Services;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace AssuranceApi.Project.Handlers;

public class CreateAssessmentHandler
{
    private readonly IProjectStandardsPersistence _assessmentPersistence;
    private readonly IProjectStandardsHistoryPersistence _historyPersistence;
    private readonly IProjectPersistence _projectPersistence;
    private readonly IServiceStandardPersistence _standardPersistence;
    private readonly IProfessionPersistence _professionPersistence;
    private readonly ILogger<CreateAssessmentHandler> _logger;

    private static readonly string[] ValidServiceStandardStatuses =
    {
        "RED",
        "AMBER",
        "GREEN",
        "TBC",
    };

    public CreateAssessmentHandler(
        IProjectStandardsPersistence assessmentPersistence,
        IProjectStandardsHistoryPersistence historyPersistence,
        IProjectPersistence projectPersistence,
        IServiceStandardPersistence standardPersistence,
        IProfessionPersistence professionPersistence,
        ILogger<CreateAssessmentHandler> logger
    )
    {
        _assessmentPersistence = assessmentPersistence;
        _historyPersistence = historyPersistence;
        _projectPersistence = projectPersistence;
        _standardPersistence = standardPersistence;
        _professionPersistence = professionPersistence;
        _logger = logger;
    }

    public async Task<AssessmentResult> HandleAsync(
        string projectId,
        string standardId,
        string professionId,
        ProjectStandards assessment
    )
    {
        try
        {
            _logger.LogInformation(
                "Processing assessment update for project {ProjectId}, standard {StandardId}, profession {ProfessionId}",
                projectId,
                standardId,
                professionId
            );

            // Validate input
            var validationResult = await ValidateAsync(
                projectId,
                standardId,
                professionId,
                assessment
            );
            if (!validationResult.IsValid)
                return validationResult;

            // Get existing assessment
            var existingAssessment = await _assessmentPersistence.GetAsync(
                projectId,
                standardId,
                professionId
            );

            // Prepare and save assessment
            PrepareAssessment(assessment, existingAssessment, projectId, standardId, professionId);
            await _assessmentPersistence.UpsertAsync(assessment);

            // Create history entry
            await CreateHistoryEntryAsync(
                assessment,
                existingAssessment,
                projectId,
                standardId,
                professionId
            );

            _logger.LogInformation("Assessment processed successfully");
            return AssessmentResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing assessment update");
            return AssessmentResult.Error($"Failed to process assessment: {ex.Message}");
        }
    }

    private async Task<AssessmentResult> ValidateAsync(
        string projectId,
        string standardId,
        string professionId,
        ProjectStandards assessment
    )
    {
        // Validate required fields
        if (string.IsNullOrEmpty(assessment.Status))
        {
            _logger.LogWarning("Assessment status is required");
            return AssessmentResult.BadRequest("Assessment status is required");
        }

        // Validate status
        if (!ValidServiceStandardStatuses.Contains(assessment.Status))
        {
            _logger.LogWarning("Invalid service standard status: {Status}", assessment.Status);
            return AssessmentResult.BadRequest(
                $"Invalid status: {assessment.Status}. Valid statuses are: RED, AMBER, GREEN, TBC"
            );
        }

        // Validate referential integrity
        var project = await _projectPersistence.GetByIdAsync(projectId);
        if (project == null)
        {
            _logger.LogWarning("Project {ProjectId} not found", projectId);
            return AssessmentResult.BadRequest("Referenced project does not exist");
        }

        var standard = await _standardPersistence.GetActiveByIdAsync(standardId);
        if (standard == null)
        {
            _logger.LogWarning("Active standard {StandardId} not found", standardId);
            return AssessmentResult.BadRequest(
                "Referenced service standard does not exist or is inactive"
            );
        }

        var profession = await _professionPersistence.GetActiveByIdAsync(professionId);
        if (profession == null)
        {
            _logger.LogWarning("Active profession {ProfessionId} not found", professionId);
            return AssessmentResult.BadRequest(
                "Referenced profession does not exist or is inactive"
            );
        }

        return AssessmentResult.Success();
    }

    private static void PrepareAssessment(
        ProjectStandards assessment,
        ProjectStandards? existingAssessment,
        string projectId,
        string standardId,
        string professionId
    )
    {
        assessment.ProjectId = projectId;
        assessment.StandardId = standardId;
        assessment.ProfessionId = professionId;
        assessment.LastUpdated = DateTime.UtcNow;

        if (existingAssessment != null)
        {
            assessment.Id = existingAssessment.Id;
            if (string.IsNullOrEmpty(assessment.ChangedBy))
                assessment.ChangedBy = existingAssessment.ChangedBy ?? "Unknown";
        }
        else
        {
            if (string.IsNullOrEmpty(assessment.Id))
                assessment.Id = ObjectId.GenerateNewId().ToString();
            if (string.IsNullOrEmpty(assessment.ChangedBy))
                assessment.ChangedBy = "Unknown";
        }
    }

    private async Task CreateHistoryEntryAsync(
        ProjectStandards assessment,
        ProjectStandards? existingAssessment,
        string projectId,
        string standardId,
        string professionId
    )
    {
        var history = new ProjectStandardsHistory
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ProjectId = projectId,
            StandardId = standardId,
            ProfessionId = professionId,
            Timestamp = DateTime.UtcNow,
            ChangedBy = assessment.ChangedBy,
            Changes = new AssessmentChanges
            {
                Status = new StatusChange
                {
                    From = existingAssessment?.Status ?? "",
                    To = assessment.Status,
                },
                Commentary = new CommentaryChange
                {
                    From = existingAssessment?.Commentary ?? "",
                    To = assessment.Commentary ?? "",
                },
            },
            Archived = false,
        };

        await _historyPersistence.AddAsync(history);
        _logger.LogInformation("Assessment history entry created successfully");
    }
}

public class AssessmentResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode { get; set; } = 200;

    public static AssessmentResult Success() => new() { IsValid = true };

    public static AssessmentResult BadRequest(string message) =>
        new()
        {
            IsValid = false,
            ErrorMessage = message,
            StatusCode = 400,
        };

    public static AssessmentResult Error(string message) =>
        new()
        {
            IsValid = false,
            ErrorMessage = message,
            StatusCode = 500,
        };
}

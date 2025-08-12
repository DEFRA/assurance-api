using AssuranceApi.Data;
using AssuranceApi.Project.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;

namespace AssuranceApi.Project.Handlers;

/// <summary>
/// Handles the creation and updating of project assessments.
/// </summary>
public class CreateAssessmentHandler
{
    /// <summary>
    /// Persistence service for project standards assessments.
    /// </summary>
    private readonly IProjectStandardsPersistence _assessmentPersistence;

    /// <summary>
    /// Persistence service for project standards history.
    /// </summary>
    private readonly IProjectStandardsHistoryPersistence _historyPersistence;

    /// <summary>
    /// Persistence service for projects.
    /// </summary>
    private readonly IProjectPersistence _projectPersistence;

    /// <summary>
    /// Persistence service for service standards.
    /// </summary>
    private readonly IServiceStandardPersistence _standardPersistence;

    /// <summary>
    /// Persistence service for professions.
    /// </summary>
    private readonly IProfessionPersistence _professionPersistence;

    /// <summary>
    /// Logger for the handler.
    /// </summary>
    private readonly ILogger<CreateAssessmentHandler> _logger;

    /// <summary>
    /// Valid statuses for service standards.
    /// </summary>
    private static readonly string[] ValidServiceStandardStatuses =
    {
        "RED",
        "AMBER",
        "GREEN",
        "TBC",
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAssessmentHandler"/> class.
    /// </summary>
    /// <param name="assessmentPersistence">The assessment persistence service.</param>
    /// <param name="historyPersistence">The history persistence service.</param>
    /// <param name="projectPersistence">The project persistence service.</param>
    /// <param name="standardPersistence">The service standard persistence service.</param>
    /// <param name="professionPersistence">The profession persistence service.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <summary>
    /// Handles the creation or update of a project assessment.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The service standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <param name="assessment">The assessment details.</param>
    /// <returns>An <see cref="AssessmentResult"/> indicating the outcome.</returns>
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

        if (!Enum.TryParse<StandardRatings>(assessment.Status, true, out _))
        {
            _logger.LogWarning($"Invalid service standard status: {assessment.Status}");

            var ratings = GetEnumNamesCsv<StandardRatings>();

            return AssessmentResult.BadRequest(
                $"Invalid status: {assessment.Status}. Valid statuses are: {ratings}"
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

    private static string GetEnumNamesCsv<TEnum>() where TEnum : Enum
    {
        return string.Join(", ", Enum.GetNames(typeof(TEnum)).Select(n => n.ToUpperInvariant()));
    }
}

/// <summary>
/// Represents the result of an assessment operation.
/// </summary>
public class AssessmentResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the error message, if any, associated with the operation.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code associated with the operation.
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Creates a successful <see cref="AssessmentResult"/>.
    /// </summary>
    /// <returns>An instance of <see cref="AssessmentResult"/> indicating success.</returns>
    public static AssessmentResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a bad request <see cref="AssessmentResult"/> with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An instance of <see cref="AssessmentResult"/> indicating a bad request.</returns>
    public static AssessmentResult BadRequest(string message) =>
        new()
        {
            IsValid = false,
            ErrorMessage = message,
            StatusCode = 400,
        };

    /// <summary>
    /// Creates an error <see cref="AssessmentResult"/> with the specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>An instance of <see cref="AssessmentResult"/> indicating an error.</returns>
    public static AssessmentResult Error(string message) =>
        new()
        {
            IsValid = false,
            ErrorMessage = message,
            StatusCode = 500,
        };
}

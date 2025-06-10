using System.Globalization;
using AssuranceApi.Profession.Services;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.ServiceStandard.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    // Project status validation - 5 RAG system + TBC
    private static readonly string[] ValidProjectStatuses = new[]
    {
        "RED",
        "AMBER_RED",
        "AMBER",
        "GREEN_AMBER",
        "GREEN",
        "TBC",
    };

    private static bool IsValidProjectStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidProjectStatuses.Contains(status);

    // Service standard status validation - 3 RAG system + TBC
    private static readonly string[] ValidServiceStandardStatuses = new[]
    {
        "RED",
        "AMBER",
        "GREEN",
        "TBC",
    };

    private static bool IsValidServiceStandardStatus(string? status) =>
        !string.IsNullOrEmpty(status) && ValidServiceStandardStatuses.Contains(status);

    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("projects", Create).RequireAuthorization("RequireAdmin");
        app.MapDelete("/projects/{id}", Delete).RequireAuthorization("RequireAdmin");
        app.MapPut("/projects/{id}", Update).RequireAuthorization("RequireAdmin");
        app.MapPut(
                "/projects/{projectId}/history/{historyId}/archive",
                async (
                    string projectId,
                    string historyId,
                    IProjectHistoryPersistence historyPersistence
                ) =>
                {
                    var success = await historyPersistence.ArchiveHistoryEntryAsync(
                        projectId,
                        historyId
                    );
                    return success ? Results.Ok() : Results.NotFound();
                }
            )
            .RequireAuthorization("RequireAdmin");

        // Read-only endpoints without authentication
        app.MapGet(
            "projects",
            async (IProjectPersistence persistence, string? tag) =>
            {
                var projects = await persistence.GetAllAsync(tag);
                return Results.Ok(projects);
            }
        );

        app.MapGet("projects/{id}", GetById);
        app.MapGet("/projects/{id}/history", GetHistory);
        app.MapGet("/projects/tags/summary", GetTagsSummary);

        // --- New endpoints for per-standard, per-profession assessment CRUD ---
        app.MapGet(
            "/projects/{projectId}/standards/{standardId}/professions/{professionId}/assessment",
            async (
                string projectId,
                string standardId,
                string professionId,
                [FromServices] IProjectStandardsPersistence assessmentPersistence
            ) =>
            {
                var assessment = await assessmentPersistence.GetAsync(
                    projectId,
                    standardId,
                    professionId
                );
                return assessment is not null ? Results.Ok(assessment) : Results.NotFound();
            }
        );

        app.MapPost(
                "/projects/{projectId}/standards/{standardId}/professions/{professionId}/assessment",
                async (
                    string projectId,
                    string standardId,
                    string professionId,
                    [FromBody] ProjectStandards assessment,
                    [FromServices] IProjectStandardsPersistence assessmentPersistence,
                    [FromServices] IProjectStandardsHistoryPersistence historyPersistence,
                    [FromServices] IProjectPersistence projectPersistence,
                    [FromServices] IServiceStandardPersistence standardPersistence,
                    [FromServices] IProfessionPersistence professionPersistence,
                    ILogger<string> logger
                ) =>
                {
                    try
                    {
                        logger.LogInformation(
                            "Processing assessment update for project {ProjectId}, standard {StandardId}, profession {ProfessionId}",
                            projectId,
                            standardId,
                            professionId
                        );

                        // Validate required fields
                        if (string.IsNullOrEmpty(assessment.Status))
                        {
                            logger.LogWarning("Assessment status is required");
                            return Results.BadRequest("Assessment status is required");
                        }

                        // Validate service standard status (3 RAG + TBC system)
                        if (!IsValidServiceStandardStatus(assessment.Status))
                        {
                            logger.LogWarning(
                                "Invalid service standard status: {Status}. Valid statuses are: RED, AMBER, GREEN, TBC",
                                assessment.Status
                            );
                            return Results.BadRequest(
                                $"Invalid status: {assessment.Status}. Valid statuses are: RED, AMBER, GREEN, TBC"
                            );
                        }

                        // **NEW: Basic referential integrity validation**

                        // Check if project exists
                        var project = await projectPersistence.GetByIdAsync(projectId);
                        if (project == null)
                        {
                            logger.LogWarning("Project {ProjectId} not found", projectId);
                            return Results.BadRequest("Referenced project does not exist");
                        }

                        // Check if standard exists and is active
                        var standard = await standardPersistence.GetActiveByIdAsync(standardId);
                        if (standard == null)
                        {
                            logger.LogWarning("Active standard {StandardId} not found", standardId);
                            return Results.BadRequest(
                                "Referenced service standard does not exist or is inactive"
                            );
                        }

                        // Check if profession exists and is active
                        var profession = await professionPersistence.GetActiveByIdAsync(
                            professionId
                        );
                        if (profession == null)
                        {
                            logger.LogWarning(
                                "Active profession {ProfessionId} not found",
                                professionId
                            );
                            return Results.BadRequest(
                                "Referenced profession does not exist or is inactive"
                            );
                        }

                        logger.LogInformation(
                            "Referential integrity validation passed for assessment"
                        );

                        // Check if assessment already exists
                        var existingAssessment = await assessmentPersistence.GetAsync(
                            projectId,
                            standardId,
                            professionId
                        );

                        // Set the IDs from URL parameters
                        assessment.ProjectId = projectId;
                        assessment.StandardId = standardId;
                        assessment.ProfessionId = professionId;
                        assessment.LastUpdated = DateTime.UtcNow;

                        // For existing assessments, preserve the existing ID and set ChangedBy if not provided
                        if (existingAssessment != null)
                        {
                            assessment.Id = existingAssessment.Id;
                            if (string.IsNullOrEmpty(assessment.ChangedBy))
                            {
                                assessment.ChangedBy = existingAssessment.ChangedBy ?? "Unknown";
                            }
                            logger.LogInformation(
                                "Updating existing assessment with ID {AssessmentId}",
                                assessment.Id
                            );
                        }
                        else
                        {
                            // Generate ID for new assessment
                            if (string.IsNullOrEmpty(assessment.Id))
                            {
                                assessment.Id = ObjectId.GenerateNewId().ToString();
                            }
                            if (string.IsNullOrEmpty(assessment.ChangedBy))
                            {
                                assessment.ChangedBy = "Unknown";
                            }
                            logger.LogInformation(
                                "Creating new assessment with ID {AssessmentId}",
                                assessment.Id
                            );
                        }

                        // Upsert assessment
                        await assessmentPersistence.UpsertAsync(assessment);
                        logger.LogInformation("Assessment upserted successfully");

                        // Create history entry with proper change tracking
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

                        await historyPersistence.AddAsync(history);
                        logger.LogInformation("Assessment history entry created successfully");

                        // Update standards summary aggregation
                        await UpdateStandardsSummaryCache(
                            projectId,
                            projectPersistence,
                            assessmentPersistence
                        );
                        logger.LogInformation("Standards summary cache updated successfully");

                        return Results.Ok();
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Error processing assessment update for project {ProjectId}, standard {StandardId}, profession {ProfessionId}",
                            projectId,
                            standardId,
                            professionId
                        );
                        return Results.Problem($"Failed to process assessment: {ex.Message}");
                    }
                }
            )
            .RequireAuthorization("RequireAdmin");

        // Assessment history endpoints
        app.MapGet(
            "/projects/{projectId}/standards/{standardId}/professions/{professionId}/history",
            async (
                string projectId,
                string standardId,
                string professionId,
                [FromServices] IProjectStandardsHistoryPersistence historyPersistence,
                ILogger<string> logger
            ) =>
            {
                logger.LogInformation(
                    "Fetching assessment history for project {ProjectId}, standard {StandardId}, profession {ProfessionId}",
                    projectId,
                    standardId,
                    professionId
                );
                var history = await historyPersistence.GetHistoryAsync(
                    projectId,
                    standardId,
                    professionId
                );
                logger.LogInformation("Found {Count} assessment history entries", history.Count);
                return Results.Ok(history);
            }
        );

        app.MapPost(
                "/projects/{projectId}/standards/{standardId}/professions/{professionId}/history/{historyId}/archive",
                async (
                    string projectId,
                    string standardId,
                    string professionId,
                    string historyId,
                    [FromServices] IProjectStandardsHistoryPersistence historyPersistence,
                    [FromServices] IProjectStandardsPersistence assessmentPersistence,
                    [FromServices] IProjectPersistence projectPersistence,
                    ILogger<string> logger
                ) =>
                {
                    logger.LogInformation(
                        "Archiving assessment history entry {HistoryId} for project {ProjectId}, standard {StandardId}, profession {ProfessionId}",
                        historyId,
                        projectId,
                        standardId,
                        professionId
                    );
                    var success = await historyPersistence.ArchiveAsync(
                        projectId,
                        standardId,
                        professionId,
                        historyId
                    );
                    if (success)
                    {
                        logger.LogInformation(
                            "Successfully archived assessment history entry, updating current assessment"
                        );

                        // Get the most recent non-archived history entry for this profession/standard
                        var remainingHistory = await historyPersistence.GetHistoryAsync(
                            projectId,
                            standardId,
                            professionId
                        );

                        if (remainingHistory.Any())
                        {
                            // Update the current assessment to reflect the most recent non-archived entry
                            var mostRecentEntry = remainingHistory.First(); // Already sorted by timestamp desc
                            var currentAssessment = await assessmentPersistence.GetAsync(
                                projectId,
                                standardId,
                                professionId
                            );

                            if (currentAssessment != null)
                            {
                                currentAssessment.Status =
                                    mostRecentEntry.Changes.Status?.To ?? currentAssessment.Status;
                                currentAssessment.Commentary =
                                    mostRecentEntry.Changes.Commentary?.To
                                    ?? currentAssessment.Commentary;
                                currentAssessment.LastUpdated = mostRecentEntry.Timestamp;
                                currentAssessment.ChangedBy = mostRecentEntry.ChangedBy;

                                await assessmentPersistence.UpsertAsync(currentAssessment);
                                logger.LogInformation(
                                    "Updated current assessment to reflect most recent non-archived entry"
                                );
                            }
                        }
                        else
                        {
                            // No remaining history - this was the only assessment, remove the current assessment
                            logger.LogInformation(
                                "No remaining history entries, removing current assessment"
                            );
                            await assessmentPersistence.DeleteAsync(
                                projectId,
                                standardId,
                                professionId
                            );
                        }

                        // Update standards summary aggregation to reflect the changes
                        await UpdateStandardsSummaryCache(
                            projectId,
                            projectPersistence,
                            assessmentPersistence
                        );
                        logger.LogInformation("Standards summary cache updated after archiving");

                        return Results.Ok();
                    }
                    else
                    {
                        logger.LogWarning(
                            "Failed to archive assessment history entry - entry not found"
                        );
                        return Results.NotFound();
                    }
                }
            )
            .RequireAuthorization("RequireAdmin");
    }

    private static async Task<IResult> Create(
        ProjectModel project,
        IProjectPersistence persistence,
        IProjectHistoryPersistence projectHistoryPersistence,
        IValidator<ProjectModel> validator
    )
    {
        // Validate status
        if (!IsValidProjectStatus(project.Status))
            return Results.BadRequest($"Invalid status: {project.Status}");
        var validationResult = await validator.ValidateAsync(project);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);

        var created = await persistence.CreateAsync(project);
        if (!created)
            return Results.BadRequest("Failed to create project");

        // Create initial history entry
        var history = new ProjectHistory
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ProjectId = project.Id,
            Timestamp = DateTime.UtcNow,
            ChangedBy = "Project created",
            Changes = new Changes
            {
                Status = new StatusChange { From = "", To = project.Status },
                Commentary = new CommentaryChange { From = "", To = project.Commentary },
            },
        };
        await projectHistoryPersistence.CreateAsync(history);

        return Results.Created($"/projects/{project.Id}", project);
    }

    private static async Task<IResult> GetById(string id, IProjectPersistence persistence)
    {
        var project = await persistence.GetByIdAsync(id);
        return project is not null ? Results.Ok(project) : Results.NotFound();
    }

    private static async Task<IResult> Update(
        string id,
        ProjectModel updatedProject,
        IProjectPersistence persistence,
        IProjectHistoryPersistence projectHistoryPersistence,
        ILogger<Program> logger,
        IValidator<ProjectModel> validator,
        HttpRequest request
    )
    {
        // Validate status
        if (!IsValidProjectStatus(updatedProject.Status))
            return Results.BadRequest($"Invalid status: {updatedProject.Status}");
        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid)
            return Results.BadRequest(validationResult.Errors);
        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null)
            return Results.NotFound();
        bool suppressHistory =
            request.Query.TryGetValue("suppressHistory", out var suppressValue)
            && suppressValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
        DateTime? updateDate = ParseUpdateDate(updatedProject.UpdateDate);
        if (!suppressHistory)
        {
            await TrackProjectChanges(
                id,
                existingProject,
                updatedProject,
                projectHistoryPersistence,
                updateDate
            );
        }

        // Preserve existing StandardsSummary and other backend-managed fields
        updatedProject.StandardsSummary = existingProject.StandardsSummary;
        updatedProject.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        if (string.IsNullOrEmpty(updatedProject.UpdateDate))
        {
            updatedProject.UpdateDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
        await UpdateProjectUpdateDate(
            existingProject,
            updatedProject,
            projectHistoryPersistence,
            id
        );
        // --- NEW: Always set main status/commentary to latest by date ---
        var allHistory = await projectHistoryPersistence.GetHistoryAsync(id);
        var latestDelivery = allHistory
            .Where(h => h.Changes?.Status != null)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefault();
        if (latestDelivery != null)
        {
            updatedProject.Status = latestDelivery.Changes.Status.To;
            updatedProject.Commentary = latestDelivery.Changes.Commentary?.To;
        }
        var updated = await persistence.UpdateAsync(id, updatedProject);
        if (!updated)
            return Results.NotFound();
        return Results.Ok(updatedProject);
    }

    // --- Helper Methods ---
    private static DateTime? ParseUpdateDate(string updateDateStr)
    {
        if (
            !string.IsNullOrEmpty(updateDateStr)
            && DateTime.TryParse(
                updateDateStr,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDate
            )
            && parsedDate <= DateTime.UtcNow
        )
            return parsedDate;
        return null;
    }

    private static async Task TrackProjectChanges(
        string id,
        ProjectModel existing,
        ProjectModel updated,
        IProjectHistoryPersistence historyPersistence,
        DateTime? updateDate
    )
    {
        var changes = new Changes();
        var hasChanges = false;
        if (existing.Name != updated.Name)
        {
            changes.Name = new NameChange { From = existing.Name, To = updated.Name };
            hasChanges = true;
        }
        if (existing.Phase != updated.Phase)
        {
            changes.Phase = new PhaseChange
            {
                From = existing.Phase ?? "",
                To = updated.Phase ?? "",
            };
            hasChanges = true;
        }
        if (existing.Status != updated.Status)
        {
            changes.Status = new StatusChange { From = existing.Status, To = updated.Status };
            hasChanges = true;
        }
        if (existing.Commentary != updated.Commentary)
        {
            changes.Commentary = new CommentaryChange
            {
                From = existing.Commentary,
                To = updated.Commentary,
            };
            hasChanges = true;
        }
        if (hasChanges)
        {
            if (changes.Status == null && changes.Commentary != null)
            {
                changes.Status = new StatusChange { From = existing.Status, To = existing.Status };
            }
            var history = new ProjectHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = id,
                Timestamp = updateDate ?? DateTime.UtcNow,
                ChangedBy = "Project Admin",
                Changes = changes,
            };
            await historyPersistence.CreateAsync(history);
        }
    }

    private static async Task UpdateProjectUpdateDate(
        ProjectModel existing,
        ProjectModel updated,
        IProjectHistoryPersistence historyPersistence,
        string id
    )
    {
        if (!string.IsNullOrEmpty(updated.UpdateDate))
        {
            var latestHistory = await historyPersistence.GetLatestHistoryAsync(id);
            var latestDate = latestHistory?.Timestamp ?? DateTime.MinValue;
            if (
                DateTime.TryParse(
                    updated.UpdateDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedUpdateDate
                )
            )
            {
                // Only allow UpdateDate if it is not before the latest history entry
                if (parsedUpdateDate < latestDate)
                {
                    updated.UpdateDate = existing.UpdateDate;
                }
            }
        }
    }

    private static async Task<IResult> Delete(string id, IProjectPersistence persistence)
    {
        var result = await persistence.DeleteAsync(id);
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> GetHistory(
        string id,
        IProjectHistoryPersistence historyPersistence
    )
    {
        var history = await historyPersistence.GetHistoryAsync(id);
        return Results.Ok(history);
    }

    private static async Task<IResult> GetTagsSummary(IProjectPersistence persistence)
    {
        var projects = await persistence.GetAllAsync();
        var summary = projects
            .SelectMany(p => p.Tags)
            .Select(tag =>
            {
                var parts = tag.Split(": ", 2);
                return new
                {
                    Category = parts[0],
                    Value = parts.Length > 1 ? parts[1] : "No Value",
                };
            })
            .GroupBy(t => t.Category)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Value).ToDictionary(sg => sg.Key, sg => sg.Count())
            );
        return Results.Ok(summary);
    }

    // --- Aggregation logic for standards summary cache ---
    private static async Task UpdateStandardsSummaryCache(
        string projectId,
        IProjectPersistence projectPersistence,
        IProjectStandardsPersistence assessmentPersistence
    )
    {
        var assessments = await assessmentPersistence.GetByProjectAsync(projectId);
        var grouped = assessments
            .GroupBy(a => a.StandardId)
            .Select(g => new StandardSummaryModel
            {
                StandardId = g.Key,
                AggregatedStatus = AggregateStatus(g.Select(x => x.Status)),
                AggregatedCommentary = string.Join(
                    "; ",
                    g.Select(x => x.Commentary).Where(c => !string.IsNullOrWhiteSpace(c))
                ),
                LastUpdated = g.Max(x => x.LastUpdated),
                Professions = g.Select(x => new StandardSummaryProfessionModel
                    {
                        ProfessionId = x.ProfessionId,
                        Status = x.Status,
                        Commentary = x.Commentary,
                        LastUpdated = x.LastUpdated,
                    })
                    .ToList(),
            })
            .ToList();
        var project = await projectPersistence.GetByIdAsync(projectId);
        if (project != null)
        {
            project.StandardsSummary = grouped;
            await projectPersistence.UpdateAsync(projectId, project);
        }
    }

    // Aggregation logic that maps 5 RAG to 3 RAG for service standards
    // Maps: AMBER_RED -> AMBER, GREEN_AMBER -> AMBER
    // Priority: RED > AMBER > GREEN > TBC
    private static string AggregateStatus(IEnumerable<string> statuses)
    {
        var mappedStatuses = statuses.Select(status =>
            status switch
            {
                "AMBER_RED" => "AMBER",
                "GREEN_AMBER" => "AMBER",
                _ => status,
            }
        );

        var order = new[] { "RED", "AMBER", "GREEN", "TBC" };
        return mappedStatuses.OrderBy(s => Array.IndexOf(order, s)).FirstOrDefault()
            ?? "NOT_UPDATED";
    }
}

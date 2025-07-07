using System.Globalization;
using AssuranceApi.Profession.Services;
using AssuranceApi.Project.Constants;
using AssuranceApi.Project.Handlers;
using AssuranceApi.Project.Helpers;
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
                    [FromServices] CreateAssessmentHandler handler,
                    [FromServices] StandardsSummaryHelper summaryHelper
                ) =>
                {
                    var result = await handler.HandleAsync(
                        projectId,
                        standardId,
                        professionId,
                        assessment
                    );

                    if (!result.IsValid)
                    {
                        return result.StatusCode == 400
                            ? Results.BadRequest(result.ErrorMessage)
                            : Results.Problem(result.ErrorMessage);
                    }

                    // Update standards summary aggregation
                    await summaryHelper.UpdateStandardsSummaryCacheAsync(projectId);

                    return Results.Ok();
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
                        var summaryHelper = new StandardsSummaryHelper(
                            projectPersistence,
                            assessmentPersistence
                        );
                        await summaryHelper.UpdateStandardsSummaryCacheAsync(projectId);
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
        if (!ProjectConstants.IsValidProjectStatus(project.Status))
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
        if (!ProjectConstants.IsValidProjectStatus(updatedProject.Status))
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
            changes.Name = new ProjectNameChange { From = existing.Name, To = updated.Name };
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
}

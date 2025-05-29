using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using AssuranceApi.Profession.Services;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("projects", Create).RequireAuthorization("RequireAuthenticated");
        app.MapDelete("/projects/{id}", Delete).RequireAuthorization("RequireAuthenticated");
        app.MapPut("/projects/{id}", Update).RequireAuthorization("RequireAuthenticated");
        app.MapPut("/projects/{projectId}/history/{historyId}/archive", async (
            string projectId,
            string historyId,
            IProjectHistoryPersistence historyPersistence) =>
        {
            var success = await historyPersistence.ArchiveHistoryEntryAsync(projectId, historyId);
            return success ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("RequireAuthenticated");
        
        // Add endpoint for archiving profession history entries
        app.MapPut("/projects/{projectId}/professions/{professionId}/history/{historyId}/archive", async (
            string projectId,
            string professionId,
            string historyId,
            IProjectProfessionHistoryPersistence historyPersistence,
            IProjectPersistence projectPersistence) =>
        {
            var success = await historyPersistence.ArchiveHistoryEntryAsync(projectId, professionId, historyId);
            if (!success) return Results.NotFound();

            // Fetch the project
            var project = await projectPersistence.GetByIdAsync(projectId);
            if (project == null) return Results.NotFound();

            // Find the latest non-archived profession history entry for this profession
            var latestNonArchived = await historyPersistence.GetLatestHistoryAsync(projectId, professionId);

            // Find the profession in the main project document
            var profession = project.Professions?.FirstOrDefault(p => p.ProfessionId == professionId);
            if (profession != null)
            {
                if (latestNonArchived != null)
                {
                    // Update the profession in the main document
                    profession.Status = latestNonArchived.Changes.Status?.To;
                    profession.Commentary = latestNonArchived.Changes.Commentary?.To;
                }
                else
                {
                    // Remove the profession if no non-archived history remains
                    project.Professions = project.Professions.Where(p => p.ProfessionId != professionId).ToList();
                }
                await projectPersistence.UpdateAsync(projectId, project);
            }
            return Results.Ok();
        }).RequireAuthorization("RequireAuthenticated");
        
        // Read-only endpoints without authentication
        app.MapGet("projects", async (IProjectPersistence persistence, string? tag) =>
        {
            var projects = await persistence.GetAllAsync(tag);
            return Results.Ok(projects);
        });
        
        app.MapGet("projects/{id}", GetById);
        app.MapGet("/projects/{id}/history", GetHistory);
        app.MapGet("/projects/tags/summary", GetTagsSummary);

        // Standard history endpoints 
        app.MapGet("/projects/{projectId}/standards/{standardId}/history", async (
            string projectId,
            string standardId,
            IStandardHistoryPersistence historyPersistence) =>
        {
            var history = await historyPersistence.GetHistoryAsync(projectId, standardId);
            return Results.Ok(history);
        });

        // Profession history endpoints
        app.MapGet("/projects/{projectId}/professions/{professionId}/history", async (
            string projectId,
            string professionId,
            IProjectProfessionHistoryPersistence historyPersistence) =>
        {
            var history = await historyPersistence.GetHistoryAsync(projectId, professionId);
            return Results.Ok(history);
        });
    }

    private static async Task<IResult> Create(
        ProjectModel project, 
        IProjectPersistence persistence,
        IProjectHistoryPersistence projectHistoryPersistence,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(project);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var created = await persistence.CreateAsync(project);
        if (!created) return Results.BadRequest("Failed to create project");

        // Create initial history entry
        var history = new ProjectHistory
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ProjectId = project.Id,
            Timestamp = DateTime.UtcNow,
            ChangedBy = "Project created",
            Changes = new Changes
            {
                Status = new StatusChange
                {
                    From = "",
                    To = project.Status
                },
                Commentary = new CommentaryChange
                {
                    From = "",
                    To = project.Commentary
                }
            }
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
        IStandardHistoryPersistence standardHistoryPersistence,
        IProjectProfessionHistoryPersistence professionHistoryPersistence,
        ILogger<Program> logger,
        IProfessionPersistence professionPersistence,
        IValidator<ProjectModel> validator,
        HttpRequest request)
    {
        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);
        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null) return Results.NotFound();
        bool suppressHistory = request.Query.TryGetValue("suppressHistory", out var suppressValue) && 
                               suppressValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
        DateTime? updateDate = ParseUpdateDate(updatedProject.UpdateDate);
        if (!suppressHistory)
        {
            await TrackProjectChanges(id, existingProject, updatedProject, projectHistoryPersistence, updateDate);
            await TrackStandardChanges(id, existingProject, updatedProject, standardHistoryPersistence, updateDate);
            await TrackProfessionChanges(id, existingProject, updatedProject, professionHistoryPersistence, professionPersistence, logger, updateDate);
        }
        await MergeProfessions(existingProject, updatedProject, professionHistoryPersistence, id, updateDate);
        updatedProject.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        await UpdateProjectUpdateDate(existingProject, updatedProject, projectHistoryPersistence, id);
        var updated = await persistence.UpdateAsync(id, updatedProject);
        if (!updated) return Results.NotFound();
        return Results.Ok(updatedProject);
    }

    // --- Helper Methods ---
    private static DateTime? ParseUpdateDate(string updateDateStr)
    {
        if (!string.IsNullOrEmpty(updateDateStr) && DateTime.TryParse(updateDateStr, out var parsedDate) && parsedDate <= DateTime.UtcNow)
            return parsedDate;
        return null;
    }

    private static async Task TrackProjectChanges(string id, ProjectModel existing, ProjectModel updated, IProjectHistoryPersistence historyPersistence, DateTime? updateDate)
    {
        var changes = new Changes();
        var hasChanges = false;
        if (existing.Name != updated.Name)
        {
            changes.Name = new NameChange { From = existing.Name, To = updated.Name };
            hasChanges = true;
        }
        if (existing.Status != updated.Status)
        {
            changes.Status = new StatusChange { From = existing.Status, To = updated.Status };
            hasChanges = true;
        }
        if (existing.Commentary != updated.Commentary)
        {
            changes.Commentary = new CommentaryChange { From = existing.Commentary, To = updated.Commentary };
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
                Changes = changes
            };
            await historyPersistence.CreateAsync(history);
        }
    }

    private static async Task TrackStandardChanges(string id, ProjectModel existing, ProjectModel updated, IStandardHistoryPersistence historyPersistence, DateTime? updateDate)
    {
        foreach (var updatedStandard in updated.Standards)
        {
            var existingStandard = existing.Standards.FirstOrDefault(s => s.StandardId == updatedStandard.StandardId);
            if (existingStandard == null) continue;
            var changes = new StandardChanges();
            var hasChanges = false;
            if (existingStandard.Status != updatedStandard.Status)
            {
                changes.Status = new StatusChange { From = existingStandard.Status, To = updatedStandard.Status };
                hasChanges = true;
            }
            if (existingStandard.Commentary != updatedStandard.Commentary)
            {
                changes.Commentary = new CommentaryChange { From = existingStandard.Commentary, To = updatedStandard.Commentary };
                hasChanges = true;
            }
            if (hasChanges)
            {
                var history = new StandardHistory
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProjectId = id,
                    StandardId = updatedStandard.StandardId,
                    Timestamp = updateDate ?? DateTime.UtcNow,
                    ChangedBy = "Standards Manager",
                    Changes = changes
                };
                await historyPersistence.CreateAsync(history);
            }
        }
    }

    private static async Task TrackProfessionChanges(string id, ProjectModel existing, ProjectModel updated, IProjectProfessionHistoryPersistence historyPersistence, IProfessionPersistence professionPersistence, ILogger logger, DateTime? updateDate)
    {
        foreach (var updatedProfession in updated.Professions)
        {
            var existingProfession = existing.Professions.FirstOrDefault(p => p.ProfessionId == updatedProfession.ProfessionId);
            bool isNew = existingProfession == null;
            var changes = new ProfessionChanges();
            var hasChanges = false;
            if (isNew)
            {
                changes.Status = new StatusChange { From = string.Empty, To = updatedProfession.Status };
                changes.Commentary = new CommentaryChange { From = string.Empty, To = updatedProfession.Commentary ?? string.Empty };
                hasChanges = true;
            }
            else
            {
                if (existingProfession.Status != updatedProfession.Status)
                {
                    changes.Status = new StatusChange { From = existingProfession.Status, To = updatedProfession.Status };
                    hasChanges = true;
                }
                if (existingProfession.Commentary != updatedProfession.Commentary)
                {
                    changes.Commentary = new CommentaryChange { From = existingProfession.Commentary ?? string.Empty, To = updatedProfession.Commentary ?? string.Empty };
                    hasChanges = true;
                }
                if (hasChanges && changes.Status == null)
                {
                    changes.Status = new StatusChange { From = existingProfession.Status, To = existingProfession.Status };
                }
            }
            if (hasChanges)
            {
                string professionName = "Unknown Profession";
                if (professionPersistence != null)
                {
                    try
                    {
                        var profession = await professionPersistence.GetByIdAsync(updatedProfession.ProfessionId);
                        if (profession != null) professionName = profession.Name;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to get profession name for {ProfessionId}", updatedProfession.ProfessionId);
                    }
                }
                var history = new ProjectProfessionHistory
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProjectId = id,
                    ProfessionId = updatedProfession.ProfessionId,
                    Timestamp = updateDate ?? DateTime.UtcNow,
                    ChangedBy = professionName,
                    Changes = changes
                };
                await historyPersistence.CreateAsync(history);
            }
        }
    }

    private static async Task MergeProfessions(ProjectModel existing, ProjectModel updated, IProjectProfessionHistoryPersistence historyPersistence, string id, DateTime? updateDate)
    {
        if (updated.Professions == null || updated.Professions.Count == 0) return;
        var currentProfessions = existing.Professions ?? new List<ProfessionModel>();
        var currentDict = currentProfessions.ToDictionary(p => p.ProfessionId, p => p);
        foreach (var updatedProfession in updated.Professions)
        {
            var latestHistory = await historyPersistence.GetLatestHistoryAsync(id, updatedProfession.ProfessionId);
            var latestDate = latestHistory?.Timestamp ?? DateTime.MinValue;
            var incomingTimestamp = updateDate ?? DateTime.UtcNow;
            if (incomingTimestamp >= latestDate)
            {
                currentDict[updatedProfession.ProfessionId] = updatedProfession;
            }
        }
        updated.Professions = currentDict.Values.ToList();
    }

    private static async Task UpdateProjectUpdateDate(ProjectModel existing, ProjectModel updated, IProjectHistoryPersistence historyPersistence, string id)
    {
        if (!string.IsNullOrEmpty(updated.UpdateDate))
        {
            var latestHistory = await historyPersistence.GetLatestHistoryAsync(id);
            var latestDate = latestHistory?.Timestamp ?? DateTime.MinValue;
            if (DateTime.TryParse(updated.UpdateDate, out var parsedUpdateDate))
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

    private static async Task<IResult> GetHistory(string id, IProjectHistoryPersistence historyPersistence)
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
                return new { Category = parts[0], Value = parts[1] };
            })
            .GroupBy(t => t.Category)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(t => t.Value)
                    .ToDictionary(
                        sg => sg.Key,
                        sg => sg.Count()
                    )
            );
        return Results.Ok(summary);
    }
}
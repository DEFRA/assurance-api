using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects", Create);
        app.MapGet("projects", async (IProjectPersistence persistence, string? tag) =>
        {
            var projects = await persistence.GetAllAsync(tag);
            return Results.Ok(projects);
        });
        app.MapGet("projects/{id}", GetById);
        app.MapPost("/projects/seedData", async (IProjectPersistence persistence, ProjectModel[] projects) =>
        {
            try
            {
                await persistence.DeleteAllAsync();
                foreach (var project in projects)
                {
                    await persistence.CreateAsync(project);
                }
                return Results.Ok("Projects seeded successfully");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to seed projects: {ex.Message}");
            }
        });
        app.MapPost("/projects/deleteAll", async (IProjectPersistence persistence) =>
        {
            try
            {
                await persistence.DeleteAllAsync();
                return Results.Ok("All projects deleted");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to delete projects: {ex.Message}");
            }
        });

        // Get history for a specific standard
        app.MapGet("/projects/{projectId}/standards/{standardId}/history", async (
            string projectId,
            string standardId,
            IStandardHistoryPersistence historyPersistence) =>
        {
            var history = await historyPersistence.GetHistoryAsync(projectId, standardId);
            return Results.Ok(history);
        });

        // Update project and track changes
        app.MapPut("/projects/{id}", Update);

        app.MapGet("/projects/{id}/history", async (
            string id,
            IProjectHistoryPersistence historyPersistence,
            ILogger<string> logger) =>
        {
            logger.LogInformation("Fetching history for project {ProjectId}", id);
            var history = await historyPersistence.GetHistoryAsync(id);
            logger.LogInformation("Found {Count} history entries for project {ProjectId}", 
                history.Count(), id);
            return Results.Ok(history);
        });

        app.MapGet("/projects/tags/summary", async (IProjectPersistence persistence) =>
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
        });
    }

    private static async Task<IResult> Create(
        ProjectModel project, 
        IProjectPersistence persistence,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(project);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var created = await persistence.CreateAsync(project);
        if (!created) return Results.BadRequest("Failed to create project");

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
        ILogger<string> logger,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null) return Results.NotFound();

        logger.LogInformation("Updating project {ProjectId}. Checking for changes...", id);

        // Track project-level changes
        var projectChanges = new Changes();
        var hasProjectChanges = false;

        // Check for name change
        if (existingProject.Name != updatedProject.Name)
        {
            logger.LogInformation("Name changed from '{OldName}' to '{NewName}'", 
                existingProject.Name, updatedProject.Name);
            projectChanges.Name = new NameChange
            {
                From = existingProject.Name,
                To = updatedProject.Name
            };
            hasProjectChanges = true;
        }

        // Check for status change
        if (existingProject.Status != updatedProject.Status)
        {
            logger.LogInformation("Status changed from '{OldStatus}' to '{NewStatus}'", 
                existingProject.Status, updatedProject.Status);
            projectChanges.Status = new StatusChange
            {
                From = existingProject.Status,
                To = updatedProject.Status
            };
            hasProjectChanges = true;
        }

        // Check for commentary change
        if (existingProject.Commentary != updatedProject.Commentary)
        {
            logger.LogInformation("Commentary changed");
            projectChanges.Commentary = new CommentaryChange
            {
                From = existingProject.Commentary,
                To = updatedProject.Commentary
            };
            hasProjectChanges = true;
        }

        // If there are project-level changes, create history record
        if (hasProjectChanges)
        {
            logger.LogInformation("Creating history record for project changes");
            var projectHistory = new ProjectHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = id,
                Timestamp = DateTime.UtcNow,
                ChangedBy = "system",
                Changes = projectChanges
            };

            var historyCreated = await projectHistoryPersistence.CreateAsync(projectHistory);
            if (!historyCreated)
            {
                logger.LogError("Failed to create project history record");
            }
        }
        else 
        {
            logger.LogInformation("No project-level changes detected");
        }

        // Track changes for each standard
        foreach (var updatedStandard in updatedProject.Standards)
        {
            var existingStandard = existingProject.Standards
                .FirstOrDefault(s => s.StandardId == updatedStandard.StandardId);

            if (existingStandard == null) continue;

            var changes = new StandardChanges();
            var hasChanges = false;

            // Check for status change
            if (existingStandard.Status != updatedStandard.Status)
            {
                changes.Status = new StatusChange
                {
                    From = existingStandard.Status,
                    To = updatedStandard.Status
                };
                hasChanges = true;
            }

            // Check for commentary change
            if (existingStandard.Commentary != updatedStandard.Commentary)
            {
                changes.Commentary = new CommentaryChange
                {
                    From = existingStandard.Commentary,
                    To = updatedStandard.Commentary
                };
                hasChanges = true;
            }

            // If there are changes, create history record
            if (hasChanges)
            {
                var history = new StandardHistory
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    ProjectId = id,
                    StandardId = updatedStandard.StandardId,
                    Timestamp = DateTime.UtcNow,
                    ChangedBy = "system",
                    Changes = changes
                };

                await standardHistoryPersistence.CreateAsync(history);
            }
        }

        var updated = await persistence.UpdateAsync(id, updatedProject);
        if (!updated) return Results.NotFound();

        return Results.Ok(updatedProject);
    }
} 
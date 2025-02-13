using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects", Create);
        app.MapGet("projects", GetAll);
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

    private static async Task<IResult> GetAll(IProjectPersistence persistence)
    {
        var projects = await persistence.GetAllAsync();
        return Results.Ok(projects);
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
        IStandardHistoryPersistence historyPersistence,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null) return Results.NotFound();

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

                await historyPersistence.CreateAsync(history);
            }
        }

        var updated = await persistence.UpdateAsync(id, updatedProject);
        if (!updated) return Results.NotFound();

        return Results.Ok(updatedProject);
    }
} 
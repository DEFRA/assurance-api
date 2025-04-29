using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("projects", Create).RequireAuthorization("RequireAuthenticated");
        app.MapPost("/projects/seedData", SeedData).RequireAuthorization("RequireAuthenticated");
        app.MapPost("/projects/deleteAll", DeleteAll).RequireAuthorization("RequireAuthenticated");
        app.MapDelete("/projects/{id}", Delete).RequireAuthorization("RequireAuthenticated");
        app.MapPut("/projects/{id}", Update).RequireAuthorization("RequireAuthenticated");
        
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

        // Add a new endpoint for adding sample projects without clearing - requires authentication
        app.MapPost("/projects/addSamples", async (
            List<ProjectModel> projects, 
            IProjectPersistence persistence,
            IProjectHistoryPersistence projectHistoryPersistence,
            IStandardHistoryPersistence standardHistoryPersistence,
            IConfiguration configuration) =>
        {
            // Add new projects without clearing existing ones
            await persistence.AddProjectsAsync(projects);
            
            // Check if history generation is enabled
            var shouldGenerateHistory = configuration["AUTO_GENERATE_HISTORY"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
            
            if (shouldGenerateHistory)
            {
                // Create historical data for each project
                foreach (var project in projects)
                {
                    await GenerateProjectHistory(project, projectHistoryPersistence, standardHistoryPersistence);
                }
            }
            
            return Results.Ok();
        }).RequireAuthorization("RequireAuthenticated");
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
        ILogger<Program> logger,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null) return Results.NotFound();

        // Track project-level changes
        var projectChanges = new Changes();
        var hasProjectChanges = false;

        // Check for name change
        if (existingProject.Name != updatedProject.Name)
        {
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
            var projectHistory = new ProjectHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = id,
                Timestamp = DateTime.UtcNow,
                ChangedBy = "system",
                Changes = projectChanges
            };

            await projectHistoryPersistence.CreateAsync(projectHistory);
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
                    Id = ObjectId.GenerateNewId().ToString(),
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

    private static async Task<IResult> Delete(string id, IProjectPersistence persistence)
    {
        var result = await persistence.DeleteAsync(id);
        return result ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> DeleteAll(IProjectPersistence persistence)
    {
        await persistence.DeleteAllAsync();
        return Results.Ok();
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

    private static async Task GenerateProjectHistory(
        ProjectModel project,
        IProjectHistoryPersistence projectHistoryPersistence,
        IStandardHistoryPersistence standardHistoryPersistence)
    {
        var statuses = new[] { "RED", "AMBER", "GREEN" };
        var random = new Random();
        
        // Sample project commentaries
        var projectCommentaries = new[] {
            "Project is progressing well with minor delays. Team has identified key bottlenecks and is working on solutions.",
            "Some risks identified but mitigation plans in place. Additional resources have been allocated to address critical areas.",
            "Major milestone achieved ahead of schedule. User feedback has been overwhelmingly positive on latest features.",
            "Resource constraints affecting delivery timeline. Working with stakeholders to reprioritize upcoming sprints.",
            "Stakeholder feedback incorporated successfully. New requirements have been documented and prioritized.",
            "Technical challenges being addressed. Architecture team reviewing proposed solutions for scalability.",
            "Budget constraints requiring reprioritization. Working with finance team to secure additional funding.",
            "Integration testing revealed performance issues. Team implementing optimizations.",
            "Security review completed successfully. Minor recommendations being implemented.",
            "User research highlighting need for accessibility improvements. UCD team leading improvements.",
            "Dependencies with external systems causing delays. Technical team engaging with third-party vendors.",
            "Sprint velocity improving after team restructure. New working patterns showing positive results."
        };

        // Sample standard commentaries
        var standardCommentaries = new[] {
            "Good progress made in implementing requirements. Team has completed all acceptance criteria.",
            "Further user research needed to validate approach. Planning sessions with target user groups.",
            "Documentation needs improvement. Technical writers being engaged to update materials.",
            "Successfully meeting accessibility requirements. WCAG 2.1 AA compliance achieved.",
            "Integration testing revealed minor issues. Team implementing fixes with automated test coverage.",
            "Positive feedback from user testing. Usability scores have improved significantly.",
            "Security review recommendations being implemented. Penetration testing scheduled.",
            "Performance metrics meeting targets. Response times within acceptable thresholds.",
            "API documentation updated to reflect latest changes. Swagger specs generated.",
            "Code quality metrics showing improvement. Static analysis tools implemented.",
            "Continuous deployment pipeline optimized. Build times reduced by 40%.",
            "Monitoring and alerting configured. On-call rotations established.",
            "Database optimization complete. Query performance improved by 60%.",
            "Mobile responsiveness issues addressed. Testing across multiple devices."
        };

        // Create historical project status changes
        for (var i = 180; i >= 0; i -= 10) // Create history every 10 days for past 180 days
        {
            var projectHistory = new ProjectHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = project.Id,
                Timestamp = DateTime.UtcNow.AddDays(-i),
                ChangedBy = new[] {
                    "DELIVERY MANAGEMENT",
                    "PRODUCT MANAGEMENT",
                    "USER CENTRED DESIGN",
                    "ARCHITECTURE",
                    "SOFTWARE DEVELOPMENT",
                    "BUSINESS ANALYSIS"
                }[random.Next(6)],
                Changes = new Changes
                {
                    Status = new StatusChange
                    {
                        From = statuses[random.Next(statuses.Length)],
                        To = i == 0 ? project.Status : statuses[random.Next(statuses.Length)]
                    },
                    Commentary = new CommentaryChange
                    {
                        From = projectCommentaries[random.Next(projectCommentaries.Length)],
                        To = projectCommentaries[random.Next(projectCommentaries.Length)]
                    }
                }
            };
            await projectHistoryPersistence.CreateAsync(projectHistory);
        }

        // Create historical standard status changes
        foreach (var standard in project.Standards)
        {
            for (var i = 180; i >= 0; i -= 15) // Create history every 15 days for past 180 days
            {
                var standardHistory = new StandardHistory
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    ProjectId = project.Id,
                    StandardId = standard.StandardId,
                    Timestamp = DateTime.UtcNow.AddDays(-i),
                    ChangedBy = new[] {
                        "DELIVERY MANAGEMENT",
                        "PRODUCT MANAGEMENT",
                        "USER CENTRED DESIGN",
                        "ARCHITECTURE",
                        "SOFTWARE DEVELOPMENT",
                        "BUSINESS ANALYSIS"
                    }[random.Next(6)],
                    Changes = new StandardChanges
                    {
                        Status = new StatusChange
                        {
                            From = statuses[random.Next(statuses.Length)],
                            To = i == 0 ? standard.Status : statuses[random.Next(statuses.Length)]
                        },
                        Commentary = new CommentaryChange
                        {
                            From = standardCommentaries[random.Next(standardCommentaries.Length)],
                            To = standardCommentaries[random.Next(standardCommentaries.Length)]
                        }
                    }
                };
                await standardHistoryPersistence.CreateAsync(standardHistory);
            }
        }
    }

    private static async Task<IResult> SeedData(
        List<ProjectModel> projects,
        IProjectPersistence persistence,
        IProjectHistoryPersistence projectHistoryPersistence,
        IStandardHistoryPersistence standardHistoryPersistence,
        HttpRequest request,
        IConfiguration configuration)
    {
        bool clearExisting = true;
        if (request.Query.TryGetValue("clearExisting", out var clearParam))
        {
            clearExisting = !string.Equals(clearParam, "false", StringComparison.OrdinalIgnoreCase);
        }

        if (clearExisting)
        {
            await persistence.DeleteAllAsync();
            await projectHistoryPersistence.DeleteAllAsync();
            await standardHistoryPersistence.DeleteAllAsync();
        }

        await persistence.SeedAsync(projects);

        // Check if history generation is enabled
        var shouldGenerateHistory = configuration["AUTO_GENERATE_HISTORY"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (shouldGenerateHistory)
        {
            foreach (var project in projects)
            {
                await GenerateProjectHistory(project, projectHistoryPersistence, standardHistoryPersistence);
            }
        }

        return Results.Ok();
    }
} 
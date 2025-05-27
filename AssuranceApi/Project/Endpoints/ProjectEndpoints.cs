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
        // Extract updateDate from the model
        DateTime? updateDate = null;
        if (!string.IsNullOrEmpty(updatedProject.UpdateDate))
        {
            if (DateTime.TryParse(updatedProject.UpdateDate, out var parsedDate))
            {
                if (parsedDate <= DateTime.UtcNow)
                    updateDate = parsedDate;
            }
        }

        logger.LogInformation("Parsed updateDate: {UpdateDate}", updateDate);

        var validationResult = await validator.ValidateAsync(updatedProject);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var existingProject = await persistence.GetByIdAsync(id);
        if (existingProject == null) return Results.NotFound();

        // Fetch latest project history for delivery updates
        var latestProjectHistory = await projectHistoryPersistence.GetLatestHistoryAsync(id);
        var latestDeliveryDate = latestProjectHistory?.Timestamp ?? DateTime.MinValue;

        // Check if history creation should be suppressed (used when synchronizing after archive)
        bool suppressHistory = request.Query.TryGetValue("suppressHistory", out var suppressValue) && 
                               suppressValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);

        if (!suppressHistory)
        {
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
                string changedBy = "Project Admin";
                // Only use updateDate from the incoming update if it was explicitly provided
                DateTime historyTimestamp;
                if (!string.IsNullOrEmpty(updatedProject.UpdateDate) && DateTime.TryParse(updatedProject.UpdateDate, out var parsedUpdateDate))
                {
                    historyTimestamp = parsedUpdateDate;
                }
                else
                {
                    historyTimestamp = DateTime.UtcNow;
                }

                // Always include the current status in the history entry if there is a commentary change but no status change
                if (projectChanges.Status == null && projectChanges.Commentary != null)
                {
                    projectChanges.Status = new StatusChange
                    {
                        From = existingProject.Status,
                        To = existingProject.Status
                    };
                }
                // Only update current status if this is the latest update
                if (historyTimestamp >= latestDeliveryDate)
                {
                    // Update current status as normal (handled after this block)
                }
                else
                {
                    // Only add to history, skip updating current status
                    var projectHistory = new ProjectHistory
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        ProjectId = id,
                        Timestamp = historyTimestamp,
                        ChangedBy = changedBy,
                        Changes = projectChanges
                    };
                    await projectHistoryPersistence.CreateAsync(projectHistory);
                    return Results.Ok(existingProject); // Do not update current status
                }

                var projectHistoryLatest = new ProjectHistory
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    ProjectId = id,
                    Timestamp = historyTimestamp,
                    ChangedBy = changedBy,
                    Changes = projectChanges
                };
                await projectHistoryPersistence.CreateAsync(projectHistoryLatest);
            }
            
            // Track changes for each standard
            foreach (var updatedStandard in updatedProject.Standards)
            {
                var existingStandard = existingProject.Standards
                    .FirstOrDefault(s => s.StandardId == updatedStandard.StandardId);

                if (existingStandard == null) continue;

                var standardChanges = new StandardChanges();
                var hasStandardChanges = false;

                // Check for status change
                if (existingStandard.Status != updatedStandard.Status)
                {
                    standardChanges.Status = new StatusChange
                    {
                        From = existingStandard.Status,
                        To = updatedStandard.Status
                    };
                    hasStandardChanges = true;
                }

                // Check for commentary change
                if (existingStandard.Commentary != updatedStandard.Commentary)
                {
                    standardChanges.Commentary = new CommentaryChange
                    {
                        From = existingStandard.Commentary,
                        To = updatedStandard.Commentary
                    };
                    hasStandardChanges = true;
                }

                // If there are changes, create history record
                if (hasStandardChanges)
                {
                    var standardHistory = new StandardHistory
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        ProjectId = id,
                        StandardId = updatedStandard.StandardId,
                        Timestamp = updateDate ?? DateTime.UtcNow,
                        ChangedBy = "Standards Manager", // Use a consistent name for standards updates
                        Changes = standardChanges
                    };

                    await standardHistoryPersistence.CreateAsync(standardHistory);
                }
            }

            // Track changes for each profession
            foreach (var updatedProfession in updatedProject.Professions)
            {
                var existingProfession = existingProject.Professions
                    .FirstOrDefault(p => p.ProfessionId == updatedProfession.ProfessionId);

                // Track if this is a new profession or an update to an existing one
                bool isNewProfession = existingProfession == null;
                
                // Fetch latest profession history for this profession
                var latestProfessionHistory = await professionHistoryPersistence.GetLatestHistoryAsync(id, updatedProfession.ProfessionId);
                var latestProfessionDate = latestProfessionHistory?.Timestamp ?? DateTime.MinValue;
                var professionHistoryTimestamp = updateDate ?? DateTime.UtcNow;
                
                // If this is a new profession, create a history record for its initial state
                if (isNewProfession)
                {
                    // Get profession name for changedBy field
                    string professionName = "Unknown Profession";
                    
                    // Try to find the profession in the database
                    if (professionPersistence != null)
                    {
                        try
                        {
                            var profession = await professionPersistence.GetByIdAsync(updatedProfession.ProfessionId);
                            if (profession != null)
                            {
                                professionName = profession.Name;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Failed to get profession name for {ProfessionId}", updatedProfession.ProfessionId);
                        }
                    }
                    
                    var newProfessionHistory = new ProjectProfessionHistory
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        ProjectId = id,
                        ProfessionId = updatedProfession.ProfessionId,
                        Timestamp = professionHistoryTimestamp,
                        ChangedBy = professionName,
                        Changes = new ProfessionChanges
                        {
                            Status = new StatusChange
                            {
                                From = string.Empty,
                                To = updatedProfession.Status
                            },
                            Commentary = new CommentaryChange
                            {
                                From = string.Empty,
                                To = updatedProfession.Commentary ?? string.Empty
                            }
                        }
                    };
                    
                    await professionHistoryPersistence.CreateAsync(newProfessionHistory);
                    // Only update current status if this is the latest
                    if (professionHistoryTimestamp < latestProfessionDate)
                    {
                        continue; // Do not update current status
                    }
                    // Otherwise, allow update below
                }
                else
                {
                    var changes = new ProfessionChanges();
                    var hasChanges = false;

                    // Check for status change
                    if (existingProfession.Status != updatedProfession.Status)
                    {
                        changes.Status = new StatusChange
                        {
                            From = existingProfession.Status,
                            To = updatedProfession.Status
                        };
                        hasChanges = true;
                    }

                    // Check for commentary change
                    if (existingProfession.Commentary != updatedProfession.Commentary)
                    {
                        changes.Commentary = new CommentaryChange
                        {
                            From = existingProfession.Commentary ?? string.Empty,
                            To = updatedProfession.Commentary ?? string.Empty
                        };
                        hasChanges = true;
                    }

                    // If there are changes, create history record
                    if (hasChanges)
                    {
                        // Get profession name for changedBy field
                        string professionName = "Unknown Profession";
                        
                        // Try to find the profession in the database
                        if (professionPersistence != null)
                        {
                            try
                            {
                                var profession = await professionPersistence.GetByIdAsync(updatedProfession.ProfessionId);
                                if (profession != null)
                                {
                                    professionName = profession.Name;
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to get profession name for {ProfessionId}", updatedProfession.ProfessionId);
                            }
                        }
                        // Always include the current status in the history entry if there is any change
                        if (changes.Status == null)
                        {
                            changes.Status = new StatusChange
                            {
                                From = existingProfession.Status,
                                To = existingProfession.Status
                            };
                        }
                        var history = new ProjectProfessionHistory
                        {
                            Id = ObjectId.GenerateNewId().ToString(),
                            ProjectId = id,
                            ProfessionId = updatedProfession.ProfessionId,
                            Timestamp = professionHistoryTimestamp,
                            ChangedBy = professionName,
                            Changes = changes
                        };
                        await professionHistoryPersistence.CreateAsync(history);
                        // Only update current status if this is the latest
                        if (professionHistoryTimestamp < latestProfessionDate)
                        {
                            continue; // Do not update current status
                        }
                        // Otherwise, allow update below
                    }
                }
                // If we reach here, update the current profession status as normal (handled after this block)
            }
        }
        else
        {
            logger.LogInformation("History creation suppressed for update of project {ProjectId}", id);
        }

        // Merge professions array to prevent old updates from overwriting current state
        if (updatedProject.Professions != null && updatedProject.Professions.Count > 0)
        {
            var currentProfessions = existingProject.Professions ?? new List<ProfessionModel>();
            var currentProfessionsDict = currentProfessions.ToDictionary(p => p.ProfessionId, p => p);

            foreach (var updatedProfession in updatedProject.Professions)
            {
                var latestProfessionHistory = await professionHistoryPersistence.GetLatestHistoryAsync(id, updatedProfession.ProfessionId);
                var latestProfessionDate = latestProfessionHistory?.Timestamp ?? DateTime.MinValue;
                var incomingTimestamp = DateTime.MinValue;
                if (!string.IsNullOrEmpty(updatedProject.UpdateDate) && DateTime.TryParse(updatedProject.UpdateDate, out var parsedDate))
                {
                    incomingTimestamp = parsedDate;
                }
                else
                {
                    incomingTimestamp = DateTime.UtcNow;
                }
                if (incomingTimestamp >= latestProfessionDate)
                {
                    currentProfessionsDict[updatedProfession.ProfessionId] = updatedProfession;
                }
            }
            updatedProject.Professions = currentProfessionsDict.Values.ToList();
        }

        // Always set lastUpdated to now (UTC) when updating the project
        updatedProject.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // Only set updateDate in the main project record if this update is the latest
        // (i.e., for delivery updates, compare to latest delivery history)
        if (!string.IsNullOrEmpty(updatedProject.UpdateDate))
        {
            var latestProjectHistoryForUpdateDate = await projectHistoryPersistence.GetLatestHistoryAsync(id);
            var latestDeliveryDateForUpdateDate = latestProjectHistoryForUpdateDate?.Timestamp ?? DateTime.MinValue;
            if (DateTime.TryParse(updatedProject.UpdateDate, out var parsedUpdateDate))
            {
                if (parsedUpdateDate < latestDeliveryDateForUpdateDate)
                {
                    // This is a historic update, do not update updateDate in the main record
                    updatedProject.UpdateDate = existingProject.UpdateDate;
                }
                // else: leave as is (will update if latest)
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
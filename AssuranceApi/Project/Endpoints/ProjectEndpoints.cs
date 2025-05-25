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
        app.MapPost("/projects/seedData", SeedData).RequireAuthorization("RequireAuthenticated");
        app.MapPost("/projects/deleteAll", DeleteAll).RequireAuthorization("RequireAuthenticated");
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
            IProjectProfessionHistoryPersistence historyPersistence) =>
        {
            var success = await historyPersistence.ArchiveHistoryEntryAsync(projectId, professionId, historyId);
            return success ? Results.Ok() : Results.NotFound();
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

        // Add a new endpoint for adding sample projects without clearing - requires authentication
        app.MapPost("/projects/addSamples", async (
            List<ProjectModel> projects, 
            IProjectPersistence persistence,
            IProjectHistoryPersistence projectHistoryPersistence,
            IStandardHistoryPersistence standardHistoryPersistence,
            IProjectProfessionHistoryPersistence professionHistoryPersistence,
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
                    await GenerateProjectHistory(project, projectHistoryPersistence, standardHistoryPersistence, professionHistoryPersistence);
                }
            }
            
            return Results.Ok();
        }).RequireAuthorization("RequireAuthenticated");
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
        IStandardHistoryPersistence standardHistoryPersistence,
        IProjectProfessionHistoryPersistence professionHistoryPersistence = null,
        AssuranceApi.Profession.Services.IProfessionPersistence professionPersistence = null)
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
        
        // Get profession data from database if available
        var professionTypes = new string[] {
            "DELIVERY MANAGEMENT",
            "PRODUCT MANAGEMENT",
            "USER CENTRED DESIGN",
            "ARCHITECTURE",
            "SOFTWARE DEVELOPMENT",
            "BUSINESS ANALYSIS"
        };
        
        // Sample profession commentaries by type
        var professionTypeCommentaries = new Dictionary<string, string[]> {
            // Delivery Management commentaries
            {"DELIVERY MANAGEMENT", new[] {
                "Sprint planning optimized with better backlog refinement sessions. Team capacity planning improved.",
                "Risk and dependency management process refined. Clear escalation paths established for blockers.",
                "Retrospectives yielding actionable improvements. Team implementing continuous improvements.",
                "Delivery metrics dashboard created to improve transparency. Burndown charts showing consistent progress.",
                "Team ceremonies restructured to reduce meeting overhead. More time for focused delivery.",
                "Cross-team dependencies identified and managed. Coordination meetings established with dependent teams.",
                "Created new process for managing technical debt alongside feature work. Technical debt being reduced.",
                "Improved estimation accuracy through team workshops. Planning more realistic and achievable."
            }},
            
            // Product Management commentaries
            {"PRODUCT MANAGEMENT", new[] {
                "Product vision refined after stakeholder workshop. Clearer direction for team.",
                "New prioritization framework implemented. Focus on highest business value features.",
                "User feedback process streamlined. Regular user testing sessions informing roadmap.",
                "Release planning optimized. More frequent, smaller releases to gather user feedback.",
                "Feature usage metrics implemented. Data-driven decisions improving user experience.",
                "Stakeholder management plan developed. Regular engagement sessions established.",
                "Competitive analysis completed. New market opportunities identified for roadmap.",
                "User personas updated with latest research. More targeted feature development."
            }},
            
            // User-Centered Design commentaries
            {"USER CENTRED DESIGN", new[] {
                "Usability testing revealing positive response to new designs. Iterating based on feedback.",
                "Design system expanded with new components. Consistent user experience across service.",
                "Accessibility audit completed. WCAG 2.1 AA compliance improvements implemented.",
                "User journey mapping workshop completed. Pain points identified for improvement.",
                "Content design patterns established. Clearer, more consistent guidance for users.",
                "Mobile-first approach refined. Responsive designs tested across device types.",
                "Information architecture review completed. Navigation simplified based on user testing.",
                "Design QA process improved. Better handoffs to development team."
            }},
            
            // Architecture commentaries
            {"ARCHITECTURE", new[] {
                "Technical architecture documented. Clear component boundaries established.",
                "Performance optimization recommendations implemented. Response times improved by 30%.",
                "Security architecture review completed. Improved authentication and authorization mechanisms.",
                "Scalability testing results positive. Architecture can handle projected growth.",
                "Technical debt review completed. Refactoring prioritized for next quarters.",
                "API design standards established. Consistent patterns across all service endpoints.",
                "Cloud infrastructure optimized. Cost reductions achieved while maintaining performance.",
                "Disaster recovery plan tested. Resilience improvements identified and implemented."
            }},
            
            // Software Development commentaries
            {"SOFTWARE DEVELOPMENT", new[] {
                "Code quality metrics showing improvement. Static analysis tools implemented.",
                "Test coverage increased to 85%. Automated test suite expanded.",
                "Continuous deployment pipeline optimized. Build times reduced by 40%.",
                "Technical documentation updated. Developer onboarding process streamlined.",
                "Pair programming sessions yielding knowledge sharing benefits. Team capability increasing.",
                "Refactoring of legacy components complete. Technical debt reduced in core modules.",
                "Frontend performance optimizations implemented. Page load times reduced significantly.",
                "Database query optimizations complete. Transaction times improved across the application."
            }},
            
            // Business Analysis commentaries
            {"BUSINESS ANALYSIS", new[] {
                "Requirements workshop yielded clear priorities. Stakeholder agreement on key features.",
                "Process modeling completed. Efficiency improvements identified and documented.",
                "User story quality improved. Acceptance criteria more specific and testable.",
                "Business case updated with latest metrics. ROI projections refined.",
                "Stakeholder matrix updated. Communication plan adjusted for key decision makers.",
                "Requirements traceability matrix created. Coverage analysis shows good test coverage.",
                "Process improvement recommendations presented to leadership. Implementation plan created.",
                "Data quality assessment completed. Recommendations made for improved validation."
            }}
        };

        // Create historical project status changes
        for (var i = 180; i >= 0; i -= 10) // Create history every 10 days for past 180 days
        {
            var projectHistory = new ProjectHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProjectId = project.Id,
                Timestamp = DateTime.UtcNow.AddDays(-i),
                ChangedBy = professionTypes[random.Next(professionTypes.Length)],
                Changes = new Changes
                {
                    Status = i % 30 == 0 ? new StatusChange // Only change status occasionally (every 30 days)
                    {
                        From = statuses[random.Next(statuses.Length)],
                        To = i == 0 ? project.Status : statuses[random.Next(statuses.Length)]
                    } : null,
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
            for (var i = 180; i >= 0; i -= 30) // Create history less frequently (every 30 days)
            {
                var standardHistory = new StandardHistory
                {
                    Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                    ProjectId = project.Id,
                    StandardId = standard.StandardId,
                    Timestamp = DateTime.UtcNow.AddDays(-i),
                    ChangedBy = professionTypes[random.Next(professionTypes.Length)],
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
        
        // Create historical profession updates if we have the repository and profession data
        if (professionHistoryPersistence != null && project.Professions != null && project.Professions.Count > 0)
        {
            // Fetch profession data from database if available
            Dictionary<string, string> professionNames = new Dictionary<string, string>();
            Dictionary<string, string[]> professionCommentaries = new Dictionary<string, string[]>();
            
            // Attempt to get profession data from database
            if (professionPersistence != null)
            {
                var allProfessions = await professionPersistence.GetAllAsync();
                
                // Create profession ID to name mapping
                foreach (var profession in allProfessions)
                {
                    professionNames[profession.Id] = profession.Name.ToUpper();
                    
                    // Assign commentaries by profession name if available
                    if (professionTypeCommentaries.ContainsKey(profession.Name.ToUpper()))
                    {
                        professionCommentaries[profession.Id] = professionTypeCommentaries[profession.Name.ToUpper()];
                    }
                }
            }

            foreach (var profession in project.Professions)
            {
                // Get profession name for changedBy (from database or fallback)
                string professionName;
                if (!professionNames.TryGetValue(profession.ProfessionId, out professionName))
                {
                    // Fallback to a default profession type if not found
                    professionName = professionTypes[random.Next(professionTypes.Length)];
                }
                
                // Get profession-specific commentaries
                string[] commentaries;
                if (!professionCommentaries.TryGetValue(profession.ProfessionId, out commentaries))
                {
                    // Fallback to random profession type commentaries if not found
                    string randomType = professionTypes[random.Next(professionTypes.Length)];
                    commentaries = professionTypeCommentaries[randomType];
                }
                
                // Create more frequent updates for professions (every 7-12 days)
                for (var i = 180; i >= 0; i -= random.Next(7, 13))
                {
                    // Determine whether this update includes a status change (about 30% of the time)
                    bool includeStatusChange = random.Next(10) < 3;
                    
                    var professionHistory = new ProjectProfessionHistory
                    {
                        Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                        ProjectId = project.Id,
                        ProfessionId = profession.ProfessionId,
                        Timestamp = DateTime.UtcNow.AddDays(-i - random.Next(3)), // Add some randomness
                        ChangedBy = professionName, // Use the profession name from database
                        Changes = new ProfessionChanges
                        {
                            // Include status change only sometimes
                            Status = includeStatusChange ? new StatusChange
                            {
                                From = statuses[random.Next(statuses.Length)],
                                To = i == 0 ? profession.Status : statuses[random.Next(statuses.Length)]
                            } : null,
                            Commentary = new CommentaryChange
                            {
                                From = i == 0 ? "" : commentaries[random.Next(commentaries.Length)],
                                To = commentaries[random.Next(commentaries.Length)]
                            }
                        }
                    };
                    await professionHistoryPersistence.CreateAsync(professionHistory);
                }
            }
        }
    }

    private static async Task<IResult> SeedData(
        List<ProjectModel> projects,
        IProjectPersistence persistence,
        IProjectHistoryPersistence projectHistoryPersistence,
        IStandardHistoryPersistence standardHistoryPersistence,
        IProjectProfessionHistoryPersistence professionHistoryPersistence,
        HttpRequest request,
        IConfiguration configuration,
        IProfessionPersistence professionPersistence)
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
            await professionHistoryPersistence.DeleteAllAsync();
        }

        // Get all professions from the database
        var professions = await professionPersistence.GetAllAsync();
        
        // Ensure each project has at least some professions
        foreach (var project in projects)
        {
            // If project doesn't have any professions, add some defaults
            if (project.Professions == null || project.Professions.Count == 0 && professions.Any())
            {
                // Create a list of default professions with random statuses
                var random = new Random();
                var statuses = new[] { "RED", "AMBER", "GREEN" };
                
                // Use the actual profession IDs from the database
                project.Professions = new List<ProfessionModel>();
                
                // Add up to 3 professions if available
                foreach (var profession in professions.Take(3))
                {
                    project.Professions.Add(new ProfessionModel
                    {
                        ProfessionId = profession.Id, // Use actual ID from database
                        Status = statuses[random.Next(statuses.Length)],
                        Commentary = $"Initial update for {profession.Name}."
                    });
                }
            }
        }

        await persistence.SeedAsync(projects);

        // Check if history generation is enabled
        var shouldGenerateHistory = configuration["AUTO_GENERATE_HISTORY"]?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
        
        if (shouldGenerateHistory)
        {
            foreach (var project in projects)
            {
                await GenerateProjectHistory(project, projectHistoryPersistence, standardHistoryPersistence, professionHistoryPersistence, professionPersistence);
            }
        }

        return Results.Ok();
    }
} 
// ProjectController.cs
using System.Globalization;
using Asp.Versioning;
using AssuranceApi.Project.Constants;
using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace AssuranceApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectPersistence _persistence;
    private readonly IProjectHistoryPersistence _historyPersistence;
    private readonly IValidator<ProjectModel> _validator;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectPersistence persistence,
        IProjectHistoryPersistence historyPersistence,
        IValidator<ProjectModel> validator,
        ILogger<ProjectsController> logger
    )
    {
        _persistence = persistence;
        _historyPersistence = historyPersistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Projects Controller");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? tag)
    {
        _logger.LogDebug("Entering get all projects API call");

        try
        {
            _logger.LogInformation("Getting all of the projects");

            var projects = await _persistence.GetAllAsync(tag);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting all the projects");
            return Problem($"Failed to get all the projects: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all projects API call");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogDebug("Entering get project by ID API call");

        try
        {
            _logger.LogInformation($"Getting all of the projects for ID='{id}'");

            var project = await _persistence.GetByIdAsync(id);
            return project is not null ? Ok(project) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the project by ID");
            return Problem($"Failed to get the project by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get project by ID API call");
        }
    }

    [HttpGet("{id}/history")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string id)
    {
        _logger.LogDebug("Entering get project history API call");

        try
        {
            _logger.LogInformation($"Getting the project history for project for ID='{id}'");

            var history = await _historyPersistence.GetHistoryAsync(id);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the project history");
            return Problem($"Failed to get the project history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get project history API call");
        }
    }

    [HttpGet("tags/summary")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTagsSummary()
    {
        _logger.LogDebug("Entering get tags summary API call");

        try
        {
            _logger.LogInformation("Getting the tags summary information'");

            var projects = await _persistence.GetAllAsync();
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
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the tags summary");
            return Problem($"Failed to get the tags summary: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get tags summary API call");
        }
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(ProjectModel project)
    {
        _logger.LogDebug("Entering create project API call");

        try
        {
            _logger.LogInformation($"Creating new project '{project.Name}'");

            if (!ProjectConstants.IsValidProjectStatus(project.Status))
            {
                var message = $"Validation failed for project status '{project.Status}'";
                _logger.LogError(message);
                return BadRequest(message);
            }

            var validationResult = await _validator.ValidateAsync(project);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the project",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var created = await _persistence.CreateAsync(project);
            if (!created)
            {
                var message = "Failed to persist the project";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

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
            await _historyPersistence.CreateAsync(history);

            return Created($"/projects/{project.Id}", project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the project");
            return Problem($"Failed to create the project: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create project API call");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(string id, ProjectModel project)
    {
        _logger.LogDebug("Entering update project API call");

        try
        {
            _logger.LogInformation($"Updating the project '{project.Name}' with ID='{id}'");

            if (!ProjectConstants.IsValidProjectStatus(project.Status))
            {
                var message = $"Validation failed for project status '{project.Status}'";
                _logger.LogError(message);
                return BadRequest(message);
            }

            var validationResult = await _validator.ValidateAsync(project);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the project",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingProject = await _persistence.GetByIdAsync(id);
            if (existingProject == null)
            {
                _logger.LogInformation($"Unable to find the '{project.Name}' with ID='{id}'");
                return NotFound();
            }

            var suppressHistory = false;

            if (Request != null)
            {
                suppressHistory =
                    Request.Query.TryGetValue("suppressHistory", out var suppressValue)
                    && suppressValue.ToString().Equals("true", StringComparison.OrdinalIgnoreCase);
            }

            var updateDate = ParseUpdateDate(project.UpdateDate);

            if (!suppressHistory)
            {
                await TrackProjectChanges(id, existingProject, project, updateDate);
            }

            project.StandardsSummary = existingProject.StandardsSummary;
            project.LastUpdated = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            project.UpdateDate ??= DateTime.UtcNow.ToString("yyyy-MM-dd");

            await UpdateProjectUpdateDate(existingProject, project, id);

            var allHistory = await _historyPersistence.GetHistoryAsync(id);
            var latestDelivery = allHistory
                .Where(h => h.Changes?.Status != null)
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefault();

            if (latestDelivery != null)
            {
                project.Status = latestDelivery.Changes.Status.To;
                project.Commentary = latestDelivery.Changes.Commentary?.To;
            }

            var updated = await _persistence.UpdateAsync(id, project);
            return updated ? Ok(project) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst updating the project");
            return Problem($"Failed to update the project: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving update project API call");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogDebug("Entering delete project API call");

        try
        {
            _logger.LogInformation($"Deleting the project with ID='{id}'");

            var result = await _persistence.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst deleting the project");
            return Problem($"Failed to update the project: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving getdelete project API call");
        }
    }

    private async Task TrackProjectChanges(
        string id,
        ProjectModel existing,
        ProjectModel updated,
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
            await _historyPersistence.CreateAsync(history);
        }
    }

    private async Task UpdateProjectUpdateDate(
        ProjectModel existing,
        ProjectModel updated,
        string id
    )
    {
        if (!string.IsNullOrEmpty(updated.UpdateDate))
        {
            var latestHistory = await _historyPersistence.GetLatestHistoryAsync(id);
            var latestDate = latestHistory?.Timestamp ?? DateTime.MinValue;
            if (
                DateTime.TryParse(
                    updated.UpdateDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsedUpdateDate
                )
                && parsedUpdateDate < latestDate
            )
            {
                updated.UpdateDate = existing.UpdateDate;
            }
        }
    }

    private DateTime? ParseUpdateDate(string updateDateStr)
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
}

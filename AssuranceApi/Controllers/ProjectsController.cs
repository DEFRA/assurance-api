// ProjectController.cs
using System.Globalization;
using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Project.Handlers;
using AssuranceApi.Project.Helpers;
using AssuranceApi.Project.Models;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace AssuranceApi.Controllers;

/// <summary>
/// API endpoints for managing projects and their assessments.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectPersistence _persistence;
    private readonly IProjectHistoryPersistence _historyPersistence;
    private readonly IProjectStandardsPersistence _projectStandardsPersistence;
    private readonly IValidator<ProjectModel> _validator;
    private readonly ILogger<ProjectsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectsController"/> class.
    /// </summary>
    /// <param name="persistence">The project persistence service.</param>
    /// <param name="historyPersistence">The project history persistence service.</param>
    /// <param name="projectStandardsPersistence">The project standards persistence service.</param>
    /// <param name="validator">The validator for <see cref="ProjectModel"/>.</param>
    /// <param name="logger">The logger for <see cref="ProjectsController"/>.</param>
    public ProjectsController(
        IProjectPersistence persistence,
        IProjectHistoryPersistence historyPersistence,
        IProjectStandardsPersistence projectStandardsPersistence,
        IValidator<ProjectModel> validator,
        ILogger<ProjectsController> logger
    )
    {
        _persistence = persistence;
        _historyPersistence = historyPersistence;
        _projectStandardsPersistence = projectStandardsPersistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Projects Controller");
    }

    /// <summary>
    /// Gets all projects.
    /// </summary>
    /// <param name="tag">Optional tag to filter projects.</param>
    /// <param name="startDate">Optional date in UTC format that limits to projects that were created or updated on or after this date</param>
    /// <param name="endDate">Optional date in UTC format that limits to projects that were created or updated before this date</param>
    /// <returns>A list of projects.</returns>
    /// <response code="200">Returns the list of projects.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProjectModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] string? tag, [FromQuery(Name = "start_date")] string? startDate, [FromQuery(Name = "end_date")] string? endDate)
    {
        _logger.LogDebug("Entering get all projects API call");

        try
        {
            _logger.LogInformation("Getting all of the projects");

            var projectQueryParameters = new ProjectQueryParameters(tag, startDate, endDate);

            var projects = await _persistence.GetAllAsync(projectQueryParameters);

            CalucalateProjectStatistics(projects);

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

    /// <summary>
    /// Gets a project by its ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>The project if found.</returns>
    /// <response code="200">Returns the project.</response>
    /// <response code="404">Project not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProjectModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogDebug("Entering get project by ID API call");

        try
        {
            _logger.LogInformation($"Getting all of the projects for ID='{id}'");

            var project = await _persistence.GetByIdAsync(id);

            if (project == null)
            {
                _logger.LogDebug($"Project with ID='{id}' not found");
                return NotFound();
            }
            
                
            _logger.LogDebug($"Found project with ID='{id}'");

            CalucalateProjectStatistics([project]);

            return Ok(project);
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

    /// <summary>
    /// Retrieves all projects associated with a specific delivery group.
    /// </summary>
    /// <param name="deliveryGroupId">The unique identifier of the delivery group.</param>
    /// <returns>A list of projects belonging to the specified delivery group.</returns>
    /// <response code="200">Returns the list of projects for the delivery group.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("bydeliverygroup/{deliveryGroupId}")]
    [ProducesResponseType(typeof(IEnumerable<ProjectModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetByDeliveryGroup(string deliveryGroupId)
    {
        _logger.LogDebug("Entering get projects by delivery group API call");

        try
        {
            _logger.LogInformation("Getting all projects for Delivery Group ID='{DeliveryGroupId}'", deliveryGroupId);

            var projects = await _persistence.GetByDeliveryGroupAsync(deliveryGroupId);

            _logger.LogDebug("Found {Count} projects for Delivery Group ID='{DeliveryGroupId}'", projects.Count, deliveryGroupId);

            // Sort projects alphabetically by name
            var sortedProjects = projects.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            CalucalateProjectStatistics(sortedProjects);

            return Ok(sortedProjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting projects by delivery group");
            return Problem($"Failed to get projects by delivery group: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get projects by delivery group API call");
        }
    }

    private static void CalucalateProjectStatistics(List<ProjectModel> projects)
    {
        if (projects == null || projects.Count == 0)
        {
            return;
        }

        foreach (var project in projects)
        {
            var projectTotalScore = CalculateProjectTotalScoreAcrossStandards(project);
            var totalNumberOfCompletedStandards = CalculateTotalNumberOfCompletedStandards(project);

            var totalNumberOfStandards = 14;
            var maxNumberStandardsScore = totalNumberOfStandards * (int)StandardRatings.Green;

            var maxNumberCompletedStandardsScore = totalNumberOfCompletedStandards * (int)StandardRatings.Green;

            project.ProjectStatus = new ProjectStatus
            {
                ScoreOfStandardsCompleted = projectTotalScore,
                PercentageAcrossAllStandards = CalculatePercentage(projectTotalScore, maxNumberStandardsScore),
                PercentageAcrossCompletedStandards = CalculatePercentage(projectTotalScore, maxNumberCompletedStandardsScore),
                NumberOfStandardsCompleted = totalNumberOfCompletedStandards,
                LowestRag = GetLowestRag(project),
            };

            project.ProjectStatus.CalculatedRag = GetCalculatedRag(project.ProjectStatus.PercentageAcrossCompletedStandards);
        }
    }

    private static string GetLowestRag(ProjectModel project)
    {
        var lowestRag = StandardRatings.Green.ToString().ToUpper();

        foreach (var standard in project.StandardsSummary)
        {
            if (standard.AggregatedStatus == StandardRatings.Amber.ToString().ToUpper())
            {
                lowestRag = StandardRatings.Amber.ToString().ToUpper();
            }
            else if (standard.AggregatedStatus == StandardRatings.Red.ToString().ToUpper())
            {
                lowestRag = StandardRatings.Red.ToString().ToUpper();
                break;
            }
        }

        return lowestRag;
    }

    private static string GetCalculatedRag(double percentageAcrossCompletedStandards)
    {
        if (percentageAcrossCompletedStandards >= 75)
            return StandardRatings.Green.ToString().ToUpper();

        else if (percentageAcrossCompletedStandards >= 50)
            return StandardRatings.Amber.ToString().ToUpper();

        return StandardRatings.Red.ToString().ToUpper();
    }

    private static int CalculateProjectTotalScoreAcrossStandards(ProjectModel project)
    {
        var total = 0;

        foreach (var standard in project.StandardsSummary)
        {
            if (standard.AggregatedStatus == StandardRatings.Green.ToString().ToUpper())
            {
                total += (int)StandardRatings.Green;
            }
            else if (standard.AggregatedStatus == StandardRatings.Amber.ToString().ToUpper())
            {
                total += (int)StandardRatings.Amber;
            }
            else if (standard.AggregatedStatus == StandardRatings.Red.ToString().ToUpper())
            {
                total += (int)StandardRatings.Red;
            }
        }

        return total;
    }

    private static int CalculateTotalNumberOfCompletedStandards(ProjectModel project)
    {
        var total = 0;

        foreach (var standard in project.StandardsSummary)
        {
            if (standard.AggregatedStatus != StandardRatings.Pending.ToString().ToUpper()
                && standard.AggregatedStatus != StandardRatings.Tbc.ToString().ToUpper()
                && standard.AggregatedStatus != StandardRatings.Excluded.ToString().ToUpper())
            {
                total ++;
            }
        }

        return total;
    }

    private static double CalculatePercentage(int top, int bottom)
    {
        if (top == 0)
            return 0;

        ArgumentOutOfRangeException.ThrowIfZero(bottom);

        return Math.Round((double)top / bottom * 100, 2);
    }

    /// <summary>
    /// Gets the history for a project.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>List of project history entries.</returns>
    /// <response code="200">Returns the project history.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id}/history")]
    [ProducesResponseType(typeof(IEnumerable<ProjectHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Archives a project history entry.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="historyId">The history entry ID.</param>
    /// <returns>Status of the archive operation.</returns>
    /// <response code="200">History entry archived successfully.</response>
    /// <response code="404">History entry not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{projectId}/history/{historyId}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteHistory(string projectId, string historyId)
    {
        // TODO: This should be a DELETE verb
        _logger.LogDebug("Entering delete project history API call");

        try
        {
            _logger.LogInformation($"Deleting the project history for project ID='{projectId}' AND history ID='{historyId}'");

            var success = await _historyPersistence.ArchiveHistoryEntryAsync(
                        projectId,
                        historyId
                    );

            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred deleting the project history");
            return Problem($"Failed to delete the project history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving delete project history API call");
        }
    }

    /// <summary>
    /// Gets a summary of all project tags.
    /// </summary>
    /// <returns>Summary of tags grouped by category and value.</returns>
    /// <response code="200">Returns the tags summary.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("tags/summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetTagsSummary()
    {
        _logger.LogDebug("Entering get tags summary API call");

        try
        {
            _logger.LogInformation("Getting the tags summary information'");

            var projects = await _persistence.GetAllAsync(new ProjectQueryParameters());
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

    /// <summary>
    /// Creates a new project.
    /// </summary>
    /// <param name="project">The project to create.</param>
    /// <returns>The newly created project.</returns>
    /// <response code="201">Returns the newly created project.</response>
    /// <response code="400">If the project is invalid or validation fails.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create(ProjectModel project)
    {
        _logger.LogDebug("Entering create project API call");

        try
        {
            _logger.LogInformation($"Creating new project '{project.Name}'");

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

    /// <summary>
    /// Updates an existing project.
    /// </summary>
    /// <param name="id">The ID of the project to update.</param>
    /// <param name="project">The updated project data.</param>
    /// <returns>The updated project if successful.</returns>
    /// <response code="200">Project updated successfully.</response>
    /// <response code="400">If the project is invalid or validation fails.</response>
    /// <response code="404">Project not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(string id, ProjectModel project)
    {
        _logger.LogDebug("Entering update project API call");

        try
        {
            _logger.LogInformation($"Updating the project '{project.Name}' with ID='{id}'");

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

            var updateDate = DateHelper.ParseUpdateDate(project.UpdateDate);

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

    /// <summary>
    /// Deletes a project by ID.
    /// </summary>
    /// <param name="id">The project ID.</param>
    /// <returns>No content if deleted.</returns>
    /// <response code="204">Project deleted successfully.</response>
    /// <response code="404">Project not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// Gets the assessment for a specific project, standard, and profession.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <returns>The assessment if found.</returns>
    /// <response code="200">Returns the assessment.</response>
    /// <response code="404">Assessment not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{projectId}/standards/{standardId}/professions/{professionId}/assessment")]
    [ProducesResponseType(typeof(ProjectStandards), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> GetProjectStandardProfessionAssessment(string projectId, string standardId, string professionId)
    {
        _logger.LogDebug("Entering get project standard professions assessment API call");

        try
        {
            _logger.LogInformation($"Getting the assessment for project ID='{projectId}' AND standard ID='{standardId}' AND profession ID='{professionId}'");

            var assessment = await _projectStandardsPersistence.GetAsync(
                projectId,
                standardId,
                professionId
        );

            return assessment is not null ? Ok(assessment) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst deleting the project");
            return Problem($"Failed to update the project: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get project standard professions assessment API call");
        }
    }

    /// <summary>
    /// Creates an assessment for a specific project, standard, and profession.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <param name="assessment">The assessment to create.</param>
    /// <param name="handler">The assessment handler.</param>
    /// <param name="summaryHelper">The standards summary helper.</param>
    /// <returns>Status of the create operation.</returns>
    /// <response code="200">Assessment created successfully.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{projectId}/standards/{standardId}/professions/{professionId}/assessment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> CreateProjectStandardProfessionAssessment(
        string projectId,
        string standardId,
        string professionId,
        [FromBody] ProjectStandards assessment,
        [FromServices] CreateAssessmentHandler handler,
        [FromServices] StandardsSummaryHelper summaryHelper)
    {
        _logger.LogDebug("Entering create project standard professions assessment API call");

        try
        {
            _logger.LogInformation($"Creating the assessment for project ID='{projectId}' AND standard ID='{standardId}' AND profession ID='{professionId}'");

            var result = await handler.HandleAsync(
                        projectId,
                        standardId,
                        professionId,
                        assessment
                    );

            if (!result.IsValid)
            {
                return result.StatusCode == 400
                    ? BadRequest(result.ErrorMessage)
                    : Problem(result.ErrorMessage);
            }

            // Update standards summary aggregation
            await summaryHelper.UpdateStandardsSummaryCacheAsync(projectId);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the project standard professions assessment");
            return Problem($"Failed to create the project standard professions assessment: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create project standard professions assessment API call");
        }
    }

    /// <summary>
    /// Gets the assessment history for a specific project, standard, and profession.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <param name="historyPersistence">The history persistence service.</param>
    /// <returns>List of assessment history entries.</returns>
    /// <response code="200">Returns the assessment history.</response>
    /// <response code="404">Assessment history not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{projectId}/standards/{standardId}/professions/{professionId}/history")]
    [ProducesResponseType(typeof(IEnumerable<ProjectStandardsHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetProjectStandardProfessionHistory(
        string projectId,
        string standardId,
        string professionId,
        IProjectStandardsHistoryPersistence historyPersistence)
    {
        _logger.LogDebug("Entering get project standard professions history API call");

        try
        {
            _logger.LogInformation($"Getting the history for project ID='{projectId}' AND standard ID='{standardId}' AND profession ID='{professionId}'");

            var history = await historyPersistence.GetHistoryAsync(
                    projectId,
                    standardId,
                    professionId
                );

            if (history == null)
            {
                return new NotFoundResult();
            }

            _logger.LogInformation($"Found '{history.Count}' assessment history entries");

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting project standard professions history");
            return Problem($"Failed to get the project standard professions history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get project standard professions history API call");
        }
    }

    /// <summary>
    /// Archives an assessment history entry for a specific project, standard, and profession.
    /// </summary>
    /// <param name="projectId">The project ID.</param>
    /// <param name="standardId">The standard ID.</param>
    /// <param name="professionId">The profession ID.</param>
    /// <param name="historyId">The history entry ID.</param>
    /// <param name="historyPersistence">The history persistence service.</param>
    /// <param name="assessmentPersistence">The assessment persistence service.</param>
    /// <param name="projectPersistence">The project persistence service.</param>
    /// <returns>Status of the archive operation.</returns>
    /// <response code="200">History entry archived successfully.</response>
    /// <response code="404">History entry not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{projectId}/standards/{standardId}/professions/{professionId}/history/{historyId}/archive")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteProjectStandardProfessionHistory(
        string projectId,
        string standardId,
        string professionId,
        string historyId,
        IProjectStandardsHistoryPersistence historyPersistence,
        [FromServices] IProjectStandardsPersistence assessmentPersistence,
        [FromServices] IProjectPersistence projectPersistence)
    {
        _logger.LogDebug("Archiving project standard professions history API call");

        try
        {
            _logger.LogInformation($"Archiving the history for project ID='{projectId}' AND standard ID='{standardId}' AND profession ID='{professionId}' AND history ID='{historyId}'");

            var success = await historyPersistence.ArchiveAsync(
                        projectId,
                        standardId,
                        professionId,
                        historyId
                    );
            if (success)
            {
                _logger.LogInformation(
                    "Successfully archived assessment history entry, updating current assessment"
                );

                // Get the most recent non-archived history entry for this profession/standard
                var remainingHistory = await historyPersistence.GetHistoryAsync(
                    projectId,
                    standardId,
                    professionId
                );

                if (remainingHistory.Count != 0)
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
                        _logger.LogInformation(
                            "Updated current assessment to reflect most recent non-archived entry"
                        );
                    }
                }
                else
                {
                    // No remaining history - this was the only assessment, remove the current assessment
                    _logger.LogInformation(
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
                _logger.LogInformation("Standards summary cache updated after archiving");

                return Ok();
            }
            else
            {
                _logger.LogError(
                    "Failed to archive assessment history entry - entry not found"
                );
                return NotFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst archiving project standard professions history");
            return Problem($"Failed to archive the project standard professions history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving archiving project standard professions history API call");
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
        if (existing.DeliveryGroupId != updated.DeliveryGroupId)
        {
            changes.DeliveryGroup = new DeliveryGroupChange
            {
                From = existing.DeliveryGroupId,
                To = updated.DeliveryGroupId,
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
}

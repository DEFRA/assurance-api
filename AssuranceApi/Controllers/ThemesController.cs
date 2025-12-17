using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Data.Models;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApi.Controllers;

/// <summary>
/// API controller for managing themes.
/// </summary>
/// <remarks>
/// Provides endpoints to create, retrieve, update, and archive themes.
/// Themes are used to group and track cross-cutting concerns across projects.
/// </remarks>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/themes")]
public class ThemesController : ControllerBase
{
    private readonly IThemePersistence _persistence;
    private readonly IValidator<ThemeModel> _validator;
    private readonly ILogger<ThemesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemesController"/> class.
    /// </summary>
    /// <param name="persistence">The persistence layer for themes.</param>
    /// <param name="validator">The validator for <see cref="ThemeModel"/>.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public ThemesController(
        IThemePersistence persistence,
        IValidator<ThemeModel> validator,
        ILogger<ThemesController> logger
    )
    {
        _persistence = persistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Themes Controller");
    }

    /// <summary>
    /// Retrieves all themes.
    /// </summary>
    /// <param name="includeArchived">Whether to include archived themes.</param>
    /// <returns>A list of all themes.</returns>
    /// <response code="200">Returns the list of themes.</response>
    /// <response code="500">An error occurred while retrieving themes.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ThemeModel>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeArchived = false)
    {
        _logger.LogDebug("Entering get all themes API call");

        try
        {
            _logger.LogInformation("Getting all themes (includeArchived: {IncludeArchived})", includeArchived);

            var themes = await _persistence.GetAllAsync(includeArchived);

            return Ok(themes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting all themes");
            return Problem($"Failed to get all themes: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all themes API call");
        }
    }

    /// <summary>
    /// Retrieves a theme by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the theme.</param>
    /// <returns>The theme if found; otherwise, NotFound.</returns>
    /// <response code="200">Returns the theme.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">An error occurred while retrieving the theme.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ThemeModel), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogDebug("Entering get theme by ID API call");

        try
        {
            _logger.LogInformation("Getting theme for ID='{Id}'", id);

            var theme = await _persistence.GetByIdAsync(id);

            if (theme == null)
            {
                _logger.LogDebug("Theme with ID='{Id}' not found", id);
                return NotFound();
            }

            _logger.LogDebug("Found theme with ID='{Id}'", id);

            return Ok(theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the theme by ID");
            return Problem($"Failed to get the theme by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get theme by ID API call");
        }
    }

    /// <summary>
    /// Creates a new theme.
    /// </summary>
    /// <param name="theme">The theme model to create.</param>
    /// <returns>The created theme model.</returns>
    /// <response code="201">Theme created successfully.</response>
    /// <response code="400">Validation errors occurred whilst creating the theme.</response>
    /// <response code="500">Failed to persist the theme due to a server error.</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ThemeModel), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Create([FromBody] ThemeModel theme)
    {
        _logger.LogDebug("Entering create theme API call");

        try
        {
            _logger.LogInformation("Creating new theme '{ThemeName}'", theme.Name);

            var validationResult = await _validator.ValidateAsync(theme);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the theme",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            // Set creation metadata
            theme.CreatedAt = DateTime.UtcNow;
            theme.UpdatedAt = DateTime.UtcNow;
            theme.CreatedBy = User.GetEmail();
            theme.UpdatedBy = User.GetEmail();
            theme.IsActive = true;

            var created = await _persistence.CreateAsync(theme);
            if (!created)
            {
                var message = "Failed to persist the theme";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            return Created($"/themes/{theme.Id}", theme);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the theme");
            return Problem($"Failed to create the theme: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create theme API call");
        }
    }

    /// <summary>
    /// Updates an existing theme.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to update.</param>
    /// <param name="theme">The updated theme details.</param>
    /// <returns>The updated theme if successful; otherwise, NotFound or BadRequest.</returns>
    /// <response code="200">Theme updated successfully.</response>
    /// <response code="400">Validation errors occurred whilst updating the theme.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">An error occurred while updating the theme.</response>
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ThemeModel), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Update(string id, [FromBody] ThemeModel theme)
    {
        _logger.LogDebug("Entering update theme API call");

        try
        {
            _logger.LogInformation("Updating the theme '{ThemeName}' with ID='{Id}'", theme.Name, id);

            var validationResult = await _validator.ValidateAsync(theme);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the theme",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingTheme = await _persistence.GetByIdAsync(id);
            if (existingTheme == null)
            {
                _logger.LogInformation("Unable to find the theme with ID='{Id}'", id);
                return NotFound();
            }

            // Preserve creation metadata and update modification metadata
            theme.Id = id;
            theme.CreatedAt = existingTheme.CreatedAt;
            theme.CreatedBy = existingTheme.CreatedBy;
            theme.UpdatedAt = DateTime.UtcNow;
            theme.UpdatedBy = User.GetEmail();

            var updated = await _persistence.UpdateAsync(id, theme);
            return updated ? Ok(theme) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst updating the theme");
            return Problem($"Failed to update the theme: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving update theme API call");
        }
    }

    /// <summary>
    /// Archives a theme by setting IsActive to false.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to archive.</param>
    /// <returns>No content if successful; otherwise, NotFound.</returns>
    /// <response code="204">Theme archived successfully.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">An error occurred while archiving the theme.</response>
    [HttpPut("{id}/archive")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Archive(string id)
    {
        _logger.LogDebug("Entering archive theme API call");

        try
        {
            _logger.LogInformation("Archiving the theme with ID='{Id}'", id);

            var result = await _persistence.ArchiveAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst archiving the theme");
            return Problem($"Failed to archive the theme: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving archive theme API call");
        }
    }

    /// <summary>
    /// Restores an archived theme by setting IsActive to true.
    /// </summary>
    /// <param name="id">The unique identifier of the theme to restore.</param>
    /// <returns>No content if successful; otherwise, NotFound.</returns>
    /// <response code="204">Theme restored successfully.</response>
    /// <response code="404">Theme not found.</response>
    /// <response code="500">An error occurred while restoring the theme.</response>
    [HttpPut("{id}/restore")]
    [Authorize]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Restore(string id)
    {
        _logger.LogDebug("Entering restore theme API call");

        try
        {
            _logger.LogInformation("Restoring the theme with ID='{Id}'", id);

            var result = await _persistence.RestoreAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst restoring the theme");
            return Problem($"Failed to restore the theme: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving restore theme API call");
        }
    }

    /// <summary>
    /// Gets all themes associated with a specific project.
    /// </summary>
    /// <param name="projectId">The project ID to filter by.</param>
    /// <returns>A list of themes associated with the project.</returns>
    /// <response code="200">Returns the list of themes for the project.</response>
    /// <response code="500">An error occurred while retrieving the themes.</response>
    [HttpGet("by-project/{projectId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<ThemeModel>), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetByProject(string projectId)
    {
        _logger.LogDebug("Entering get themes by project API call");

        try
        {
            _logger.LogInformation("Getting themes for project ID='{ProjectId}'", projectId);

            var themes = await _persistence.GetByProjectIdAsync(projectId);

            return Ok(themes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting themes for project");
            return Problem($"Failed to get themes for project: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get themes by project API call");
        }
    }
}


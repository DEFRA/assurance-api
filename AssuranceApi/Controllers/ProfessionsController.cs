using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Profession.Models;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System;

namespace AssuranceApi.Controllers;

/// <summary>
/// API endpoints for managing professions.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/professions")]
public class ProfessionsController : ControllerBase
{
    private readonly IProfessionPersistence _persistence;
    private readonly IProfessionHistoryPersistence _historyPersistence;
    private readonly IValidator<ProfessionModel> _validator;
    private readonly ILogger<ProfessionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfessionsController"/> class.
    /// </summary>
    /// <param name="persistence">The profession persistence service.</param>
    /// <param name="historyPersistence">The profession history persistence service.</param>
    /// <param name="validator">The validator for <see cref="ProfessionModel"/>.</param>
    /// <param name="logger">The logger for the controller.</param>
    public ProfessionsController(
       IProfessionPersistence persistence,
       IProfessionHistoryPersistence historyPersistence,
       IValidator<ProfessionModel> validator,
       ILogger<ProfessionsController> logger
    )
    {
        _persistence = persistence;
        _historyPersistence = historyPersistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Professions Controller");
    }

    /// <summary>
    /// Creates a new profession.
    /// </summary>
    /// <param name="profession">The profession to create.</param>
    /// <returns>The newly created profession.</returns>
    /// <response code="201">Returns the newly created profession.</response>
    /// <response code="400">If the profession is invalid or validation fails.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProfessionModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create([FromBody] ProfessionModel profession)
    {
        _logger.LogDebug("Entering create profession API call");

        try
        {
            var validationResult = await _validator.ValidateAsync(profession);
            if (!validationResult.IsValid)
            {
                var message =
                    $"Validation failed for create profession API call '{validationResult.Errors}'";
                _logger.LogError(message);
                return BadRequest(message);
            }

            _logger.LogInformation($"Creating profession '{profession.Name}'");

            var created = await _persistence.CreateAsync(profession);
            if (!created)
            {
                var message = "Failed to persist the profession";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            var getRoute = $"/api/v1.0/professions/{profession.Id}";
            return Created(getRoute, profession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the profession");
            return Problem($"Failed to create the profession: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create profession API call");
        }
    }

    /// <summary>
    /// Updates an existingProfession profession.
    /// </summary>
    /// <param name="id">The ID of the profession to update.</param>
    /// <param name="profession">The updated profession data.</param>
    /// <returns>The updated profession if successful.</returns>
    /// <response code="200">Returns the updated profession.</response>
    /// <response code="400">If the profession is invalid or validation fails.</response>
    /// <response code="404">If the profession is not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProfessionModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] ProfessionModel profession)
    {
        _logger.LogDebug("Entering update profession API call");

        try
        {
            if (profession == null)
            {
                var message = $"Invalid profession with ID '{id}'";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            if (id != profession.Id)
            {
                var message = "ID in URL does not match ID in body";
                _logger.LogError(message);
                return BadRequest(message);
            }

            var validationResult = await _validator.ValidateAsync(profession);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the standard",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingProfession = await _persistence.GetByIdAsync(id);
            if (existingProfession is null)
            {
                _logger.LogWarning($"Profession with ID '{id}' not found for update");
                return NotFound();
            }

            profession.UpdatedAt = DateTime.UtcNow;

            await TrackChanges(id, existingProfession, profession);

            var created = await _persistence.UpdateAsync(profession);
            if (!created)
            {
                var message = $"Failed to persist updated profession with ID '{id}'";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            _logger.LogInformation($"Updated profession '{profession.Name}' with ID '{id}'");
            return Ok(profession);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst updating the profession");
            return Problem($"Failed to update the profession: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving update profession API call");
        }
    }

    /// <summary>
    /// Deletes all professions.
    /// </summary>
    /// <returns>Status of the delete operation.</returns>
    /// <response code="200">All professions deleted successfully.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("deleteAll")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteAll()
    {
        _logger.LogDebug("Entering delete all professions API call");

        try
        {
            _logger.LogInformation("Deleting all professions");

            await _persistence.DeleteAllAsync();
            return Ok("All professions deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst deleting the professions");
            return Problem($"Failed to delete all professions: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving deleta all profession API call");
        }
    }

    /// <summary>
    /// Seeds the professions collection with the provided array.
    /// </summary>
    /// <param name="professions">Array of professions to seed.</param>
    /// <returns>Status of the seed operation.</returns>
    /// <response code="200">Professions seeded successfully.</response>
    /// <response code="400">Validation errors occurred.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Seed([FromBody] ProfessionModel[] professions)
    {
        _logger.LogDebug("Entering seed all professions API call");

        try
        {
            _logger.LogInformation($"Seeding '{professions.Length}' professions");

            var validationErrors = await ValidateProfessions(professions);
            if (validationErrors.Any())
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst seeding the prfoessions",
                    validationErrors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var createdCount = await CreateProfessions(professions);

            _logger.LogInformation($"Successfully seeded '{createdCount}' professions");

            return Ok(new { Message = $"Seeded '{createdCount}' professions successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed professions");
            return Problem($"Failed to seed professions: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving seed all profession API call");
        }
    }

    /// <summary>
    /// Soft deletes a profession by ID.
    /// </summary>
    /// <param name="id">The profession ID.</param>
    /// <returns>Status of the delete operation.</returns>
    /// <response code="200">Profession soft deleted successfully.</response>
    /// <response code="404">Profession not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SoftDelete(string id)
    {
        _logger.LogDebug("Entering soft delete profession API call");

        try
        {
            _logger.LogInformation($"Soft deleting profession with ID '{id}'");

            var success = await _persistence.SoftDeleteAsync(id, "System");
            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete profession");
            return Problem($"Failed to soft delete profession: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving soft delete profession API call");
        }
    }

    /// <summary>
    /// Restores a soft-deleted profession by ID.
    /// </summary>
    /// <param name="id">The profession ID.</param>
    /// <returns>Status of the restore operation.</returns>
    /// <response code="200">Profession restored successfully.</response>
    /// <response code="404">Profession not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Restore(string id)
    {
        _logger.LogDebug("Entering restore profession API call");

        try
        {
            _logger.LogInformation($"Restoring profession with ID '{id}'");

            var success = await _persistence.RestoreAsync(id);
            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore profession");
            return Problem($"Failed to restore profession: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving restore profession API call");
        }
    }

    /// <summary>
    /// Gets all professions.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive professions.</param>
    /// <returns>List of professions.</returns>
    /// <response code="200">Returns the list of professions.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProfessionModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        _logger.LogDebug("Entering get all professions API call");

        try
        {
            _logger.LogInformation($"Getting professions where Inactive= '{includeInactive}'");

            var professions = includeInactive
                ? await _persistence.GetAllAsync()
                : await _persistence.GetAllActiveAsync();
            return Ok(professions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get professions");
            return Problem($"Failed to get professions: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all professions API call");
        }
    }

    /// <summary>
    /// Gets a profession by ID.
    /// </summary>
    /// <param name="id">The profession ID.</param>
    /// <param name="includeInactive">Whether to include inactive professions.</param>
    /// <returns>The profession if found.</returns>
    /// <response code="200">Returns the profession.</response>
    /// <response code="404">Profession not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProfessionModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id, [FromQuery] bool includeInactive = false)
    {
        _logger.LogDebug("Entering get profession by ID API call");

        try
        {
            _logger.LogInformation(
                $"Getting profession where ID='{id}' and Inactive='{includeInactive}'"
            );

            var profession = includeInactive
                ? await _persistence.GetByIdAsync(id)
                : await _persistence.GetActiveByIdAsync(id);
            return profession is not null ? Ok(profession) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profession by ID");
            return Problem($"Failed to get profession by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get profession by ID API call");
        }
    }

    /// <summary>
    /// Gets the history for a profession.
    /// </summary>
    /// <param name="professionId">The profession ID.</param>
    /// <returns>List of profession history entries.</returns>
    /// <response code="200">Returns the profession history.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{professionId}/history")]
    [ProducesResponseType(typeof(IEnumerable<ProfessionHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string professionId)
    {
        _logger.LogDebug("Entering get profession history API call");

        try
        {
            _logger.LogInformation($"Fetching history for profession '{professionId}'");

            var history = await _historyPersistence.GetHistoryAsync(professionId);

            _logger.LogInformation(
                $"Found '{history.Count()}' history entries for ID='{professionId}'"
            );

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profession history");
            return Problem($"Failed to get profession history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get profession history API call");
        }
    }

    private async Task<List<string>> ValidateProfessions(ProfessionModel[] professions)
    {
        var validationErrors = new List<string>();

        foreach (var profession in professions)
        {
            var validationResult = await _validator.ValidateAsync(profession);
            if (!validationResult.IsValid)
            {
                validationErrors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
            }
        }

        return validationErrors;
    }

    private async Task<int> CreateProfessions(ProfessionModel[] professions)
    {
        var createdCount = 0;

        foreach (var profession in professions)
        {
            var created = await _persistence.CreateAsync(profession);
            if (created)
                createdCount++;
        }

        return createdCount;
    }

    private async Task TrackChanges(
        string id,
        ProfessionModel existing,
        ProfessionModel updated
    )
    {
        var changes = new ProfessionChanges();
        var hasChanges = false;

        if (existing.Name != updated.Name)
        {
            changes.Name = new NameChange { From = existing.Name, To = updated.Name };
            hasChanges = true;
        }
        if (existing.Description != updated.Description)
        {
            changes.Description = new DescriptionChange { From = existing.Description, To = updated.Description };
            hasChanges = true;
        }
        if (existing.IsActive != updated.IsActive)
        {
            changes.IsActive = new ActivityChange
            {
                From = existing.IsActive.ToString(),
                To = updated.IsActive.ToString(),
            };
            hasChanges = true;
        }

        if (hasChanges)
        {
            var history = new ProfessionHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ProfessionId = id,
                Timestamp = updated.UpdatedAt,
                ChangedBy = User.GetEmail() ?? "Unknown User",
                Changes = changes,
            };
            await _historyPersistence.CreateAsync(history);
        }
    }
}

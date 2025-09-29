using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Project.Models;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.Utils;
using Elastic.CommonSchema;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AssuranceApi.Controllers;

/// <summary>
/// API endpoints for managing service standards.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/servicestandards")]
public class ServiceStandardsController : ControllerBase
{
    private readonly IServiceStandardPersistence _persistence;
    private readonly IServiceStandardHistoryPersistence _historyPersistence;
    private readonly IValidator<ServiceStandardModel> _validator;
    private readonly ILogger<ServiceStandardsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceStandardsController"/> class.
    /// </summary>
    /// <param name="persistence">The service newStandard persistence service.</param>
    /// <param name="historyPersistence">The service newStandard history persistence service.</param>
    /// <param name="validator">The validator for <see cref="ServiceStandardModel"/> instances.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public ServiceStandardsController(
        IServiceStandardPersistence persistence,
        IServiceStandardHistoryPersistence historyPersistence,
        IValidator<ServiceStandardModel> validator,
        ILogger<ServiceStandardsController> logger
    )
    {
        _persistence = persistence;
        _historyPersistence = historyPersistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating ServiceStandards Controller");
    }

    /// <summary>
    /// Seeds the service standards collection with the provided list.
    /// </summary>
    /// <param name="standards">List of service standards to seed.</param>
    /// <returns>Status of the seed operation.</returns>
    /// <response code="200">Service standards seeded successfully or all deleted if list is empty.</response>
    /// <response code="400">Failed to seed service standards.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Seed([FromBody] List<ServiceStandardModel> standards)
    {
        _logger.LogDebug("Entering seed all service standards API call");

        try
        {
            _logger.LogInformation($"Seeding '{standards.Count}' standards");

            if (!standards.Any())
            {
                await _persistence.DeleteAllAsync();
                return Ok();
            }

            var created = await _persistence.SeedStandardsAsync(standards);
            return created ? Ok() : BadRequest("Failed to seed service standards");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed service standards");
            return Problem($"Failed to seed service standards: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving seed all service standards API call");
        }
    }

    /// <summary>
    /// Creates a new service newStandard.
    /// </summary>
    /// <param name="standard">The service newStandard to create.</param>
    /// <returns>The updated service newStandard.</returns>
    /// <response code="201">Service newStandard updated successfully.</response>
    /// <response code="400">Invalid service newStandard data.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceStandardModel), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Create([FromBody] ServiceStandardModel standard)
    {
        _logger.LogDebug("Entering create service standard API call");

        try
        {
            if (standard == null)
            {
                return BadRequest("Service standard data is required.");
            }

            var validationResult = await _validator.ValidateAsync(standard);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the standard",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            if (await IsServiceStandardADuplicate(standard))
            {
                return Conflict("A service standard with the same number already exists.");
            }

            standard.CreatedAt = DateTime.UtcNow;
            standard.UpdatedAt = standard.CreatedAt;
            standard.IsActive = true;

            var created = await _persistence.CreateAsync(standard);
            if (!created)
            {
                return BadRequest("Failed to create service standard.");
            }

            var history = new ServiceStandardHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                StandardId = standard.Id,
                Timestamp = DateTime.UtcNow,
                ChangedBy = User.GetEmail(),
                ChangedByName = User.GetName(),
                Changes = new ServiceStandardChanges
                {
                    Name = new ServiceStandardNameChange { From = "", To = standard.Name },
                    Description = new ServiceStandardDescriptionChange { From = "", To = standard.Description },
                    Guidance = new ServiceStandardGuidanceChange { From = "", To = standard.Guidance }
                },
            };
            await _historyPersistence.CreateAsync(history);

            _logger.LogInformation($"Created service standard with ID='{standard.Id}'");
            return Created($"/servicestandards/{standard.Id}", standard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service standard");
            return Problem($"Failed to create service standard: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create service standard API call");
        }
    }

    /// <summary>
    /// Updates an existing service standard.
    /// </summary>
    /// <param name="id">The ID of the service standard to update.</param>
    /// <param name="standard">The updated service standard data.</param>
    /// <returns>The updated service standard if successful.</returns>
    /// <response code="200">Service standard updated successfully.</response>
    /// <response code="400">If the service standard is invalid or validation fails.</response>
    /// <response code="404">Service standard not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProjectModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Update(string id, [FromBody] ServiceStandardModel standard)
    {
        _logger.LogDebug("Entering update service standard API call");

        try
        {
            _logger.LogInformation($"Updating service standard with ID='{id}'");

            if (standard == null)
            {
                var message = $"Invalid service standard with ID '{id}'";
                _logger.LogError(message);
                return BadRequest(message);
            }

            if (id != standard.Id)
            {
                var message = "ID in URL does not match ID in body";
                _logger.LogError(message);
                return BadRequest(message);
            }

            var validationResult = await _validator.ValidateAsync(standard);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the standard",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingServiceStandard = await _persistence.GetByIdAsync(id);
            if (existingServiceStandard == null)
            {
                _logger.LogInformation($"Unable to find the service standard with ID='{id}'");
                return NotFound();
            }

            standard.UpdatedAt = DateTime.UtcNow;

            await TrackChanges(id, existingServiceStandard, standard);

            var updated = await _persistence.UpdateAsync(standard);
            if (!updated)
            {
                return BadRequest("Failed to create service standard.");
            }

            _logger.LogInformation($"Updated service standard with ID='{standard.Id}'");
            return Ok(standard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service standard");
            return Problem($"Failed to create service standard: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create service standard API call");
        }
    }

    /// <summary>
    /// Deletes all service standards.
    /// </summary>
    /// <returns>Status of the delete operation.</returns>
    /// <response code="200">All service standards deleted successfully.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("deleteAll")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> DeleteAll()
    {
        _logger.LogDebug("Entering delete all service standards API call");

        try
        {
            _logger.LogInformation("Deleting all service standards");

            await _persistence.DeleteAllAsync();
            return Ok("All service standards deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete all service standards");
            return Problem($"Failed to delete all service standards: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving delete all service standards API call");
        }
    }

    /// <summary>
    /// Soft deletes a service newStandard by ID.
    /// </summary>
    /// <param name="id">The service newStandard ID.</param>
    /// <returns>Status of the delete operation.</returns>
    /// <response code="200">Service newStandard soft deleted successfully.</response>
    /// <response code="404">Service newStandard not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> SoftDelete(string id)
    {
        _logger.LogDebug("Entering soft delete service standard API call");

        try
        {
            _logger.LogInformation($"Soft deleting service standard with ID='{id}'");

            var success = await _persistence.SoftDeleteAsync(id, "System");
            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to soft delete service standard");
            return Problem($"Failed to soft delete service standard: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving soft delete service standard API call");
        }
    }

    /// <summary>
    /// Restores a soft-deleted service newStandard by ID.
    /// </summary>
    /// <param name="id">The service newStandard ID.</param>
    /// <returns>Status of the restore operation.</returns>
    /// <response code="200">Service newStandard restored successfully.</response>
    /// <response code="404">Service newStandard not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPost("{id}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<IActionResult> Restore(string id)
    {
        _logger.LogDebug("Entering restore service standard API call");

        try
        {
            _logger.LogInformation($"Restoring service standard with ID='{id}'");

            var success = await _persistence.RestoreAsync(id);
            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore service standard");
            return Problem($"Failed to restore service standard: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving restore service standard API call");
        }
    }

    /// <summary>
    /// Gets all service standards.
    /// </summary>
    /// <param name="includeInactive">Whether to include inactive service standards.</param>
    /// <returns>List of service standards.</returns>
    /// <response code="200">Returns the list of service standards.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ServiceStandardModel>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        _logger.LogDebug("Entering get all service standards API call");

        try
        {
            _logger.LogInformation("Getting all service standards");

            var standards = includeInactive
                ? await _persistence.GetAllAsync()
                : await _persistence.GetAllActiveAsync();
            return Ok(standards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all service standards");
            return Problem($"Failed to get all service standards: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all service standard API call");
        }
    }

    /// <summary>
    /// Gets a service newStandard by ID.
    /// </summary>
    /// <param name="id">The service newStandard ID.</param>
    /// <param name="includeInactive">Whether to include inactive service standards.</param>
    /// <returns>The service newStandard if found.</returns>
    /// <response code="200">Returns the service newStandard.</response>
    /// <response code="404">Service newStandard not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceStandardModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(string id, [FromQuery] bool includeInactive = false)
    {
        _logger.LogDebug("Entering get service standard by ID API call");

        try
        {
            _logger.LogInformation($"Getting service standard by ID='{id}'");

            var standard = includeInactive
                ? await _persistence.GetByIdAsync(id)
                : await _persistence.GetActiveByIdAsync(id);
            return standard is not null ? Ok(standard) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service standard by ID");
            return Problem($"Failed to get service standard by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get service standard by ID API call");
        }
    }

    /// <summary>
    /// Gets the history for a service newStandard.
    /// </summary>
    /// <param name="standardId">The service standard ID.</param>
    /// <returns>List of service newStandard history entries.</returns>
    /// <response code="200">Returns the service newStandard history.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{standardId}/history")]
    [ProducesResponseType(typeof(IEnumerable<ServiceStandardHistory>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string standardId)
    {
        _logger.LogDebug("Entering get service standard history API call");

        try
        {
            _logger.LogInformation($"Getting service standard history by ID='{standardId}'");

            var history = await _historyPersistence.GetHistoryAsync(standardId);
            _logger.LogInformation(
                "Found {Count} history entries for standard {StandardId}",
                history.Count(),
                standardId
            );
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get service standard history");
            return Problem($"Failed to get service standard history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get service standard history API call");
        }
    }

    private async Task<bool> IsServiceStandardADuplicate(ServiceStandardModel newStandard)
    {
        return await _persistence.GetByIdAsync(newStandard.Id) != null;
    }

    private async Task TrackChanges(
        string id,
        ServiceStandardModel existing,
        ServiceStandardModel updated
    )
    {
        var changes = new ServiceStandardChanges();
        var hasChanges = false;

        if (existing.Name != updated.Name)
        {
            changes.Name = new ServiceStandardNameChange { From = existing.Name, To = updated.Name };
            hasChanges = true;
        }
        if (existing.Description != updated.Description)
        {
            changes.Description = new ServiceStandardDescriptionChange { From = existing.Description, To = updated.Description };
            hasChanges = true;
        }
        if (existing.Guidance != updated.Guidance)
        {
            changes.Guidance = new ServiceStandardGuidanceChange { From = existing.Guidance, To = updated.Guidance };
            hasChanges = true;
        }
        if (existing.IsActive != updated.IsActive)
        {
            changes.IsActive = new ServiceStandardActivityChange
            {
                From = existing.IsActive.ToString(),
                To = updated.IsActive.ToString(),
            };
            hasChanges = true;
        }

        if (hasChanges)
        { 
            var history = new ServiceStandardHistory
            {
                Id = ObjectId.GenerateNewId().ToString(),
                StandardId = id,
                Timestamp = updated.UpdatedAt,
                ChangedBy = User.GetEmail(),
                ChangedByName = User.GetName(),
                Changes = changes,
            };
            await _historyPersistence.CreateAsync(history);
        }
    }
}

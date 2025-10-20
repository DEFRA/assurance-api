using Asp.Versioning;
using AssuranceApi.Data.Models;
using AssuranceApi.Utils;
using Microsoft.AspNetCore.Mvc;
using AssuranceApi.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using AssuranceApi.Project.Models;
using AssuranceApi.Data.ChangeHistory;
using MongoDB.Bson;

namespace AssuranceApi.Controllers;

/// <summary>
/// API controller for managing delivery groups.
/// </summary>
/// <remarks>
/// Provides endpoints to create, retrieve, update, and delete delivery groups.
/// </remarks>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/deliverygroups")]
public class DeliveryGroupsController : ControllerBase
{
    private readonly IDeliveryGroupPersistence _persistence;
    private readonly IProjectPersistence _projectPersistence;
    private readonly IHistoryPersistence<DeliveryGroupChanges> _historyPersistence;
    private readonly IValidator<DeliveryGroupModel> _validator;
    private readonly ILogger<DeliveryGroupsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryGroupsController"/> class.
    /// </summary>
    /// <param name="persistence">The persistence layer for delivery groups.</param>
    /// <param name="projectPersistence"></param>
    /// <param name="validator">The validator for <see cref="DeliveryGroupModel"/>.</param>
    /// <param name="historyPersistence">The persistence layer for tracking delivery group changes.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public DeliveryGroupsController(
        IDeliveryGroupPersistence persistence,
        IProjectPersistence projectPersistence,
        IHistoryPersistence<DeliveryGroupChanges> historyPersistence,
        IValidator<DeliveryGroupModel> validator,
        ILogger<DeliveryGroupsController> logger
    )
    {
        _persistence = persistence;
        _projectPersistence = projectPersistence;
        _historyPersistence = historyPersistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Delivery Group Controller");
    }

    /// <summary>
    /// Creates a new delivery group.
    /// </summary>
    /// <param name="deliveryGroup">The delivery group model to create.</param>
    /// <returns>The created delivery group model.</returns>
    /// <response code="201">Delivery group created successfully.</response>
    /// <response code="400">Validation errors occurred whilst creating the delivery group.</response>
    /// <response code="500">Failed to persist the delivery group due to a server error.</response>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(DeliveryGroupModel), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Create([FromBody] DeliveryGroupModel deliveryGroup)
    {
        _logger.LogDebug("Entering create delivery group API call");

        try
        {
            _logger.LogInformation("Creating new delivery group '{DeliveryGroupName}'", deliveryGroup.Name);

            var validationResult = await _validator.ValidateAsync(deliveryGroup);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the delivery group",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var created = await _persistence.CreateAsync(deliveryGroup);
            if (!created)
            {
                var message = "Failed to persist the delivery group";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            return Created($"/deliverygroups/{deliveryGroup.Id}", deliveryGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the delivery group");
            return Problem($"Failed to create the delivery group: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create delivery group API call");
        }
    }

    /// <summary>
    /// Retrieves all delivery groups.
    /// </summary>
    /// <returns>A list of all delivery groups.</returns>
    /// <response code="200">Returns the list of delivery groups.</response>
    /// <response code="500">An error occurred while retrieving delivery groups.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DeliveryGroupModel), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogDebug("Entering get all delivery groups API call");

        try
        {
            _logger.LogInformation("Getting all of the delivery groups");

            var groups = await _persistence.GetAllAsync();

            return Ok(groups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting all the delivery groups");
            return Problem($"Failed to get all the delivery groups: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all delivery groups API call");
        }
    }

    /// <summary>
    /// Retrieves a delivery group by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery group.</param>
    /// <returns>The delivery group if found; otherwise, NotFound.</returns>
    /// <response code="200">Returns the delivery group.</response>
    /// <response code="404">Delivery group not found.</response>
    /// <response code="500">An error occurred while retrieving the delivery group.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DeliveryGroupModel), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogDebug("Entering get delivery group by ID API call");

        try
        {
            _logger.LogInformation("Getting delivery group for ID='{Id}'", id);

            var deliveryGroup = await _persistence.GetByIdAsync(id);

            if (deliveryGroup == null)
            {
                _logger.LogDebug("Delivery group with ID='{Id}' not found", id);
                return NotFound();
            }

            _logger.LogDebug("Found delivery group with ID='{Id}'", id);

            return Ok(deliveryGroup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the delivery group by ID");
            return Problem($"Failed to get the delivery group by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get delivery group by ID API call");
        }
    }

    /// <summary>
    /// Retrieves all projects associated with a specific delivery group.
    /// </summary>
    /// <param name="deliveryGroupId">The unique identifier of the delivery group.</param>
    /// <returns>A list of projects associated with the delivery group.</returns>
    /// <response code="200">Returns the list of projects associated with the delivery group.</response>
    /// <response code="404">Delivery group not found.</response>
    /// <response code="500">An error occurred while retrieving the projects.</response>
    [HttpGet("{deliveryGroupId}/projects")]
    [ProducesResponseType(typeof(List<ProjectModel>), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    [AllowAnonymous]
    public async Task<IActionResult> GetDeliveryGroupProjects(string deliveryGroupId)
    {
        _logger.LogDebug($"Entering get delivery group projects API call for delivery group ID {deliveryGroupId}");
        try
        {
            _logger.LogInformation($"Getting delivery group projects for ID='{deliveryGroupId}'");

            var projectQueryParameters = new ProjectQueryParameters
            {
                DeliveryGroupId = deliveryGroupId
            };

            var deliveryGroupProjects = await _projectPersistence.GetAllAsync(projectQueryParameters);

            return Ok(deliveryGroupProjects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred whilst getting the delivery group projects for delivery group ID {deliveryGroupId}");
            return Problem($"Failed to get the delivery group projects: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get delivery group projects API call");
        }
    }

    /// <summary>
    /// Updates an existing delivery group.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery group to update.</param>
    /// <param name="deliveryGroup">The updated delivery group details.</param>
    /// <returns>The updated delivery group if successful; otherwise, NotFound or BadRequest.</returns>
    /// <response code="200">Delivery group updated successfully.</response>
    /// <response code="400">Validation errors occurred whilst updating the delivery group.</response>
    /// <response code="404">Delivery group not found.</response>
    /// <response code="500">An error occurred while updating the delivery group.</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(DeliveryGroupModel), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Update(string id, [FromBody] DeliveryGroupModel deliveryGroup)
    {
        _logger.LogDebug("Entering update delivery group API call");

        try
        {
            _logger.LogInformation("Updating the delivery group '{DeliveryGroupName}' with ID='{Id}'", deliveryGroup.Name, id);

            var validationResult = await _validator.ValidateAsync(deliveryGroup);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the delivery group",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingDeliveryGroup = await _persistence.GetByIdAsync(id);
            if (existingDeliveryGroup == null)
            {
                _logger.LogInformation("Unable to find the delivery group with ID='{Id}'", id);
                return NotFound();
            }

            deliveryGroup.UpdatedAt = DateTime.UtcNow;
            await TrackChanges(id, existingDeliveryGroup, deliveryGroup);

            var updated = await _persistence.UpdateAsync(id, deliveryGroup);
            return updated ? Ok(deliveryGroup) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst updating the delivery group");
            return Problem($"Failed to update the delivery group: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving update delivery group API call");
        }
    }

    /// <summary>
    /// Deletes a delivery group by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery group to delete.</param>
    /// <returns>No content if successful; otherwise, NotFound.</returns>
    /// <response code="204">Delivery group deleted successfully.</response>
    /// <response code="404">Delivery group not found.</response>
    /// <response code="500">An error occurred while deleting the delivery group.</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogDebug("Entering delete delivery group API call");

        try
        {
            _logger.LogInformation("Deleting the delivery group with ID='{Id}'", id);

            var result = await _persistence.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst deleting the delivery group");
            return Problem($"Failed to delete the delivery group: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving delete delivery group API call");
        }
    }

    /// <summary>
    /// Gets the history for a delivery group.
    /// </summary>
    /// <param name="deliveryGroupId">The delivery group ID.</param>
    /// <returns>List of delivery group history entries.</returns>
    /// <response code="200">Returns the delivery group history.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("{deliveryGroupId}/history")]
    [ProducesResponseType(typeof(IEnumerable<History<DeliveryGroupChanges>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string deliveryGroupId)
    {
        _logger.LogDebug("Entering get delivery group history API call");

        try
        {
            _logger.LogInformation($"Getting the delivery group history for delivery group for ID='{deliveryGroupId}'");

            var history = await _historyPersistence.GetHistoryAsync(deliveryGroupId);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the delivery group history");
            return Problem($"Failed to get the delivery group history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get delivery group history API call");
        }
    }

    /// <summary>
    /// Archives a delivery group history entry.
    /// </summary>
    /// <param name="deliveryGroupId">The delivery group ID.</param>
    /// <param name="historyId">The history entry ID.</param>
    /// <returns>Status of the archive operation.</returns>
    /// <response code="200">History entry archived successfully.</response>
    /// <response code="404">History entry not found.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpPut("{deliveryGroupId}/history/{historyId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteHistory(string deliveryGroupId, string historyId)
    {
        // TODO: This should be a DELETE verb
        _logger.LogDebug("Entering delete delivery group history API call");

        try
        {
            _logger.LogInformation($"Deleting the project history for project ID='{deliveryGroupId}' AND history ID='{historyId}'");

            var success = await _historyPersistence.ArchiveHistoryEntryAsync(
                        deliveryGroupId,
                        historyId
                    );

            return success ? Ok() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred deleting the delivery group history");
            return Problem($"Failed to delete the delivery group history: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving delete delivery group history API call");
        }
    }

    private async Task TrackChanges(string id, DeliveryGroupModel existing, DeliveryGroupModel updated
    )
    {
        var changes = new DeliveryGroupChanges();
        var hasChanges = false;

        if (existing.Name != updated.Name)
        {
            changes.Name = new Change<string> { From = existing.Name, To = updated.Name };
            hasChanges = true;
        }
        if (existing.Lead != updated.Lead)
        {
            changes.Lead = new Change<string> { From = existing.Lead, To = updated.Lead };
            hasChanges = true;
        }
        if (existing.Outcome != updated.Outcome)
        {
            changes.Outcome = new Change<string> { From = existing.Outcome!, To = updated.Outcome! };
            hasChanges = true;
        }
        if (existing.RoadmapName != updated.RoadmapName)
        {
            changes.Outcome = new Change<string> { From = existing.RoadmapName!, To = updated.RoadmapName! };
            hasChanges = true;
        }
        if (existing.RoadmapLink != updated.RoadmapLink)
        {
            changes.Outcome = new Change<string> { From = existing.RoadmapLink!, To = updated.RoadmapLink! };
            hasChanges = true;
        }
        if (existing.IsActive != updated.IsActive)
        {
            changes.IsActive = new Change<bool> { From = existing.IsActive, To = updated.IsActive };
            hasChanges = true;
        }

        if (hasChanges)
        {
            var history = new History<DeliveryGroupChanges>
            {
                Id = ObjectId.GenerateNewId().ToString(),
                ItemId = id,
                Timestamp = updated.UpdatedAt,
                ChangedBy = User.GetEmail(),
                ChangedByName = User.GetName(),
                Changes = changes,
            };
            await _historyPersistence.CreateAsync(history);
        }
    }
}

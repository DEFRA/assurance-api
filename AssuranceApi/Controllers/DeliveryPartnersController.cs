using Asp.Versioning;
using AssuranceApi.Project.Models;
using AssuranceApi.Data.Models;
using AssuranceApi.Utils;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Threading.Tasks;
using AssuranceApi.Controllers;
using AssuranceApi.Data;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace AssuranceApi.Controllers;

/// <summary>
/// API controller for managing delivery partners.
/// </summary>
/// <remarks>
/// Provides endpoints to create, retrieve, update, and delete delivery partners.
/// </remarks>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/deliverypartners")]
public class DeliveryPartnersController : ControllerBase
{
    private readonly IDeliveryPartnerPersistence _persistence;
    private readonly IValidator<DeliveryPartnerModel> _validator;
    private readonly ILogger<DeliveryPartnersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeliveryPartnersController"/> class.
    /// </summary>
    /// <param name="persistence">The persistence layer for delivery partners.</param>
    /// <param name="validator">The validator for <see cref="DeliveryPartnerModel"/>.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public DeliveryPartnersController(
        IDeliveryPartnerPersistence persistence,
        IValidator<DeliveryPartnerModel> validator,
        ILogger<DeliveryPartnersController> logger
    )
    {
        _persistence = persistence;
        _validator = validator;
        _logger = logger;

        _logger.LogDebug("Creating Delivery Partner Controller");
    }

    /// <summary>
    /// Creates a new delivery partner.
    /// </summary>
    /// <param name="deliveryPartner">The delivery partner model to create.</param>
    /// <returns>The created delivery partner model.</returns>
    /// <response code="201">Delivery partner created successfully.</response>
    /// <response code="400">Validation errors occurred whilst creating the delivery partner.</response>
    /// <response code="500">Failed to persist the delivery partner due to a server error.</response>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(DeliveryPartnerModel), 201)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Create([FromBody] DeliveryPartnerModel deliveryPartner)
    {
        _logger.LogDebug("Entering create delivery partner API call");

        try
        {
            _logger.LogInformation($"Creating new delivery partner '{deliveryPartner.Name}'");

            var validationResult = await _validator.ValidateAsync(deliveryPartner);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the delivery partner",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var created = await _persistence.CreateAsync(deliveryPartner);
            if (!created)
            {
                var message = "Failed to persist the delivery partner";
                _logger.LogError(message);
                return StatusCode(500, message);
            }

            return Created($"/deliverypartners/{deliveryPartner.Id}", deliveryPartner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst creating the delivery partner");
            return Problem($"Failed to create the delivery partner: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving create delivery partner API call");
        }
    }

    /// <summary>
    /// Retrieves all delivery partners.
    /// </summary>
    /// <returns>A list of all delivery partners.</returns>
    /// <response code="200">Returns the list of delivery partners.</response>
    /// <response code="500">An error occurred while retrieving delivery partners.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DeliveryPartnerModel), 200)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogDebug("Entering get all delivery partners API call");

        try
        {
            _logger.LogInformation("Getting all of the delivery partners");

            var partners = await _persistence.GetAllAsync();

            return Ok(partners);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting all the delivery partners");
            return Problem($"Failed to get all the delivery partners: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get all delivery partners API call");
        }
    }

    /// <summary>
    /// Retrieves a delivery partner by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery partner.</param>
    /// <returns>The delivery partner if found; otherwise, NotFound.</returns>
    /// <response code="200">Returns the delivery partner.</response>
    /// <response code="404">Delivery partner not found.</response>
    /// <response code="500">An error occurred while retrieving the delivery partner.</response>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DeliveryPartnerModel), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> GetById(string id)
    {
        _logger.LogDebug("Entering get delivery partner by ID API call");

        try
        {
            _logger.LogInformation($"Getting delivery partner for ID='{id}'");

            var deliveryPartner = await _persistence.GetByIdAsync(id);

            if (deliveryPartner == null)
            {
                _logger.LogDebug($"Delivery partner with ID='{id}' not found");
                return NotFound();
            }

            _logger.LogDebug($"Found delivery partner with ID='{id}'");

            return Ok(deliveryPartner);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting the delivery partner by ID");
            return Problem($"Failed to get the delivery partner by ID: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get delivery partner by ID API call");
        }
    }

    /// <summary>
    /// Updates an existing delivery partner.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery partner to update.</param>
    /// <param name="deliveryPartner">The updated delivery partner details.</param>
    /// <returns>The updated delivery partner if successful; otherwise, NotFound or BadRequest.</returns>
    /// <response code="200">Delivery partner updated successfully.</response>
    /// <response code="400">Validation errors occurred whilst updating the delivery partner.</response>
    /// <response code="404">Delivery partner not found.</response>
    /// <response code="500">An error occurred while updating the delivery partner.</response>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(DeliveryPartnerModel), 200)]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Update(string id, [FromBody] DeliveryPartnerModel deliveryPartner)
    {
        _logger.LogDebug("Entering update delivery partner API call");

        try
        {
            _logger.LogInformation($"Updating the delivery partner '{deliveryPartner.Name}' with ID='{id}'");

            var validationResult = await _validator.ValidateAsync(deliveryPartner);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst updating the delivery partner",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            var existingPartner = await _persistence.GetByIdAsync(id);
            if (existingPartner == null)
            {
                _logger.LogInformation($"Unable to find the delivery partner with ID='{id}'");
                return NotFound();
            }

            deliveryPartner.UpdatedAt = DateTime.UtcNow;

            var updated = await _persistence.UpdateAsync(id, deliveryPartner);
            return updated ? Ok(deliveryPartner) : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst updating the delivery partner");
            return Problem($"Failed to update the delivery partner: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving update delivery partner API call");
        }
    }

    /// <summary>
    /// Deletes a delivery partner by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the delivery partner to delete.</param>
    /// <returns>No content if successful; otherwise, NotFound.</returns>
    /// <response code="204">Delivery partner deleted successfully.</response>
    /// <response code="404">Delivery partner not found.</response>
    /// <response code="500">An error occurred while deleting the delivery partner.</response>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(string), 500)]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogDebug("Entering delete delivery partner API call");

        try
        {
            _logger.LogInformation($"Deleting the delivery partner with ID='{id}'");

            var result = await _persistence.DeleteAsync(id);
            return result ? NoContent() : NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst deleting the delivery partner");
            return Problem($"Failed to delete the delivery partner: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving delete delivery partner API call");
        }
    }
}

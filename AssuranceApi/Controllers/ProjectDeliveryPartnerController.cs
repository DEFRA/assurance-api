using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Data.Models;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace AssuranceApi.Controllers
{
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:ApiVersion}/projects/")]
    public class ProjectDeliveryPartnerController : ControllerBase
    {
        private readonly IProjectDeliveryPartnerPersistence _persistence;
        private readonly IProjectPersistence _projectPersistence;
        private readonly IDeliveryPartnerPersistence _deliveryPartnerPersistence;
        private readonly IValidator<ProjectDeliveryPartnerModel> _validator;
        private readonly ILogger<ProjectDeliveryPartnerController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectDeliveryPartnerController"/> class.
        /// </summary>
        /// <param name="persistence">The persistence layer for project delivery partners.</param>
        /// <param name="projectPersistence">The persistence layer for projects.</param>
        /// <param name="deliveryPartnerPersistence">The persistence layer for delivery partners.</param>
        /// <param name="validator">The validator for <see cref="ProjectDeliveryPartnerModel"/>.</param>
        /// <param name="logger">The logger for the controller.</param>
        public ProjectDeliveryPartnerController(
            IProjectDeliveryPartnerPersistence persistence,
            IProjectPersistence projectPersistence,
            IDeliveryPartnerPersistence deliveryPartnerPersistence,
            IValidator<ProjectDeliveryPartnerModel> validator,
            ILogger<ProjectDeliveryPartnerController> logger
        )
        {
            _persistence = persistence;
            _validator = validator;
            _projectPersistence = projectPersistence;
            _deliveryPartnerPersistence = deliveryPartnerPersistence;
            _logger = logger;

            _logger.LogDebug("Creating ProjectDeliveryPartner Controller");
        }

        /// <summary>
        /// Gets all delivery partners associated with a specific project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <returns>A list of delivery partners for the specified project.</returns>
        /// <response code="200">Returns the list of delivery partners.</response>
        /// <response code="400">If the project ID is null or empty.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{projectId}/deliverypartners")]
        [ProducesResponseType(typeof(IEnumerable<ProjectDeliveryPartnerModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [AllowAnonymous]
        public async Task<IActionResult> GetProjectDeliveryPartnersByProjectId(string projectId)
        {
            _logger.LogDebug("Entering get delivery partners by project ID API call");

            try
            {
                if (string.IsNullOrWhiteSpace(projectId))
                {
                    _logger.LogWarning("Project ID is null or empty");
                    return BadRequest("Project ID cannot be null or empty.");
                }

                _logger.LogInformation("Getting all of the get delivery partners by project");

                var deliveryPartners = await _persistence.GetByProjectAsync(projectId);

                return Ok(deliveryPartners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred whilst getting the delivery partners by project ID");
                return Problem($"Failed to get all the delivery partners: {ex.Message}");
            }
            finally
            {
                _logger.LogDebug("Leaving get delivery partners by project ID API call");
            }
        }

        /// <summary>
        /// Gets a specific delivery partner associated with a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="deliveryPartnerId">The unique identifier of the delivery partner.</param>
        /// <returns>The delivery partner for the specified project if found.</returns>
        /// <response code="200">Returns the delivery partner.</response>
        /// <response code="400">If the project ID or delivery partner ID is null or empty.</response>
        /// <response code="404">If the delivery partner is not found for the project.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{projectId}/deliverypartners/{deliveryPartnerId}")]
        [ProducesResponseType(typeof(ProjectDeliveryPartnerModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [AllowAnonymous]
        public async Task<IActionResult> GetProjectDeliveryPartner(string projectId, string deliveryPartnerId)
        {
            _logger.LogDebug("Entering get delivery partner by project ID and delivery partner ID API call");

            try
            {
                if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(deliveryPartnerId))
                {
                    _logger.LogWarning("Project ID or Delivery Partner ID is null or empty");
                    return BadRequest("Project ID and Delivery Partner ID cannot be null or empty.");
                }

                _logger.LogInformation("Getting delivery partner by project and delivery partner ID");

                var deliveryPartner = await _persistence.GetAsync(projectId, deliveryPartnerId);

                if (deliveryPartner == null)
                {
                    _logger.LogWarning("Delivery partner not found for project ID {ProjectId} and delivery partner ID {DeliveryPartnerId}", projectId, deliveryPartnerId);
                    return NotFound();
                }

                return Ok(deliveryPartner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred whilst getting the delivery partner by project ID and delivery partner ID");
                return Problem($"Failed to get the delivery partner: {ex.Message}");
            }
            finally
            {
                _logger.LogDebug("Leaving get delivery partner by project ID and delivery partner ID API call");
            }
        }

        /// <summary>
        /// Creates or updates a delivery partner association for a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project to associate with the delivery partner.</param>
        /// <param name="projectDeliveryPartner">The delivery partner association details to create or update.</param>
        /// <returns>The created or updated delivery partner association for the project.</returns>
        /// <remarks>
        /// This endpoint creates a new association between a project and a delivery partner.
        /// The request body must include the project ID and delivery partner ID.
        /// Requires admin authorization.
        /// </remarks>
        /// <response code="201">Delivery partner association created or updated successfully. Returns the created or updated association.</response>
        /// <response code="400">The input is invalid, such as missing required fields or validation errors.</response>
        /// <response code="404">The specified project or delivery partner does not exist.</response>
        /// <response code="500">An internal server error occurred while processing the request.</response>
        [HttpPost("{projectId}/deliverypartners")]
        [ProducesResponseType(typeof(ProjectDeliveryPartnerModel), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateProjectDeliveryPartner(string projectId, [FromBody] ProjectDeliveryPartnerModel projectDeliveryPartner)
        {
            _logger.LogDebug("Entering create delivery partner API call");

            try
            {
                var validationResult = await ValidateUpsertDetails(projectId, projectDeliveryPartner);

                if (validationResult != null)
                {
                    return validationResult;
                }

                projectDeliveryPartner.Id = ObjectId.GenerateNewId().ToString();

                await _persistence.UpsertAsync(projectDeliveryPartner);

                return Created($"/projects/{projectId}/deliverypartners/{projectDeliveryPartner.DeliveryPartnerId}", projectDeliveryPartner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred whilst creating the delivery partner association");
                return Problem($"Failed to create the delivery partner association: {ex.Message}");
            }
            finally
            {
                _logger.LogDebug("Leaving create or update delivery partner API call");
            }
        }

        /// <summary>
        /// Updates an existing delivery partner association for a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="deliveryPartnerId">The unique identifier of the delivery partner.</param>
        /// <param name="projectDeliveryPartner">The updated delivery partner association details.</param>
        /// <returns>The updated delivery partner association for the project.</returns>
        /// <response code="200">Delivery partner association updated successfully. Returns the updated association.</response>
        /// <response code="400">The input is invalid, such as missing required fields or validation errors.</response>
        /// <response code="404">The specified project or delivery partner association does not exist.</response>
        /// <response code="500">An internal server error occurred while processing the request.</response>
        [HttpPut("{projectId}/deliverypartners/{deliveryPartnerId}")]
        [ProducesResponseType(typeof(ProjectDeliveryPartnerModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateProjectDeliveryPartner(string projectId, string deliveryPartnerId, [FromBody] ProjectDeliveryPartnerModel projectDeliveryPartner)
        {
            _logger.LogDebug("Entering update project delivery partner API call");

            try
            {
                var validationResult = await ValidateUpsertDetails(projectId, projectDeliveryPartner);

                if (validationResult != null)
                {
                    return validationResult;
                }

                if (projectDeliveryPartner.DeliveryPartnerId != deliveryPartnerId)
                {
                    _logger.LogWarning("Invalid input for delivery partner creation projectDeliveryPartner.DeliveryPartnerId != deliveryPartnerId");
                    return BadRequest("DeliveryPartnerId IDs must match and cannot be null or empty.");
                }

                await _persistence.UpsertAsync(projectDeliveryPartner);

                return Ok(projectDeliveryPartner);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred whilst updating the delivery partner association");
                return Problem($"Failed to update the delivery partner association: {ex.Message}");
            }
            finally
            {
                _logger.LogDebug("Leaving update delivery partner API call");
            }
        }

        private async Task<IActionResult?> ValidateUpsertDetails(string projectId, ProjectDeliveryPartnerModel projectDeliveryPartner)
        {
            if (string.IsNullOrWhiteSpace(projectId) || projectDeliveryPartner == null)
            {
                _logger.LogWarning("Invalid input for delivery partner creation");
                return BadRequest("Project ID and ProjectDeliveryPartnerModel cannot be null or empty.");
            }

            if (projectDeliveryPartner.ProjectId != projectId)
            {
                _logger.LogWarning("Invalid input for delivery partner creation projectDeliveryPartner.ProjectId != projectId");
                return BadRequest("Project IDs must match and cannot be null or empty.");
            }

            var validationResult = await _validator.ValidateAsync(projectDeliveryPartner);
            if (!validationResult.IsValid)
            {
                var message = ValidationHelper.GetValidationMessage(
                    "Validation errors occurred whilst creating the project delivery partner",
                    validationResult.Errors
                );
                _logger.LogError(message);
                return BadRequest(message);
            }

            _logger.LogDebug("Getting project and delivery partner details in parallel");

            var projectTask = _projectPersistence.GetByIdAsync(projectId);
            var deliveryPartnerTask = _deliveryPartnerPersistence.GetByIdAsync(projectDeliveryPartner.DeliveryPartnerId);

            await Task.WhenAll(projectTask, deliveryPartnerTask);

            var project = projectTask.Result;
            var deliveryPartner = deliveryPartnerTask.Result;

            if (project == null || deliveryPartner == null)
            {
                var message = $"Project and/or DeliveryPartner not found for project ID '{projectId}' and delivery partner ID '{projectDeliveryPartner.DeliveryPartnerId}'";
                _logger.LogWarning(message);
                return NotFound(message);
            }

            return null;
        }

        /// <summary>
        /// Deletes a delivery partner association for a project.
        /// </summary>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="deliveryPartnerId">The unique identifier of the delivery partner.</param>
        /// <returns>No content if deleted successfully.</returns>
        /// <response code="204">Delivery partner association deleted successfully.</response>
        /// <response code="400">If the project ID or delivery partner ID is null or empty.</response>
        /// <response code="404">If the delivery partner association is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpDelete("{projectId}/deliverypartners/{deliveryPartnerId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteProjectDeliveryPartner(string projectId, string deliveryPartnerId)
        {
            _logger.LogDebug("Entering delete delivery partner API call");

            try
            {
                if (string.IsNullOrWhiteSpace(projectId) || string.IsNullOrWhiteSpace(deliveryPartnerId))
                {
                    _logger.LogWarning("Project ID or Delivery Partner ID is null or empty");
                    return BadRequest("Project ID and Delivery Partner ID cannot be null or empty.");
                }

                var deleted = await _persistence.DeleteAsync(projectId, deliveryPartnerId);

                if (!deleted)
                {
                    _logger.LogWarning("Delivery partner association not found for project ID {ProjectId} and delivery partner ID {DeliveryPartnerId}", projectId, deliveryPartnerId);
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred whilst deleting the delivery partner association");
                return Problem($"Failed to delete the delivery partner association: {ex.Message}");
            }
            finally
            {
                _logger.LogDebug("Leaving delete delivery partner API call");
            }
        }
    }
}

using Asp.Versioning;
using AssuranceApi.Data;
using AssuranceApi.Insights.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApi.Controllers;

/// <summary>
/// API endpoints for insights and prioritisation data.
/// </summary>
[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/insights")]
public class InsightsController : ControllerBase
{
    private readonly IInsightsPersistence _persistence;
    private readonly ILogger<InsightsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="InsightsController"/> class.
    /// </summary>
    /// <param name="persistence">The insights persistence service.</param>
    /// <param name="logger">The logger for <see cref="InsightsController"/>.</param>
    public InsightsController(
        IInsightsPersistence persistence,
        ILogger<InsightsController> logger
    )
    {
        _persistence = persistence;
        _logger = logger;

        _logger.LogDebug("Creating Insights Controller");
    }

    /// <summary>
    /// Gets prioritisation data for weekly meetings.
    /// Returns deliveries needing attention based on update history and worsening standards.
    /// </summary>
    /// <param name="standardThreshold">Days threshold for considering a delivery stale (default: 14).</param>
    /// <param name="worseningDays">Days to look back for worsening standards (default: 14).</param>
    /// <returns>Prioritisation response with categorised deliveries.</returns>
    /// <response code="200">Returns the prioritisation data.</response>
    /// <response code="500">If an internal server error occurs.</response>
    [HttpGet("prioritisation")]
    [ProducesResponseType(typeof(PrioritisationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> GetPrioritisation(
        [FromQuery] int standardThreshold = 14,
        [FromQuery] int worseningDays = 14)
    {
        _logger.LogDebug("Entering get prioritisation API call");

        try
        {
            _logger.LogInformation(
                "Getting prioritisation data with standardThreshold={StandardThreshold} and worseningDays={WorseningDays}",
                standardThreshold,
                worseningDays
            );

            var deliveriesNeedingUpdates = await _persistence.GetDeliveriesNeedingStandardUpdatesAsync(standardThreshold);
            var deliveriesWithWorseningStandards = await _persistence.GetDeliveriesWithWorseningStandardsAsync(worseningDays);

            _logger.LogInformation(
                "Found {NeedingUpdates} deliveries needing updates and {Worsening} deliveries with worsening standards",
                deliveriesNeedingUpdates.Count,
                deliveriesWithWorseningStandards.Count
            );

            var response = new PrioritisationResponse
            {
                DeliveriesNeedingStandardUpdates = deliveriesNeedingUpdates,
                DeliveriesWithWorseningStandards = deliveriesWithWorseningStandards
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred whilst getting prioritisation data");
            return Problem($"Failed to get prioritisation data: {ex.Message}");
        }
        finally
        {
            _logger.LogDebug("Leaving get prioritisation API call");
        }
    }
}


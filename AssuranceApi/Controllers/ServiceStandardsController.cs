using Asp.Versioning;
using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/servicestandards")]
public class ServiceStandardsController : ControllerBase
{
    private readonly IServiceStandardPersistence _persistence;
    private readonly IServiceStandardHistoryPersistence _historyPersistence;
    private readonly ILogger<ServiceStandardsController> _logger;

    public ServiceStandardsController(
        IServiceStandardPersistence persistence,
        IServiceStandardHistoryPersistence historyPersistence,
        ILogger<ServiceStandardsController> logger)
    {
        _persistence = persistence;
        _historyPersistence = historyPersistence;
        _logger = logger;

        _logger.LogDebug("Creating ServiceStandards Controller");
    }


    [HttpPost("seed")]
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


    [HttpPost("deleteAll")]
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


    [HttpDelete("{id}")]
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


    [HttpPost("{id}/restore")]
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


    [HttpGet]
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


    [HttpGet("{id}")]
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


    [HttpGet("{standardId}/history")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHistory(string id)
    {
        _logger.LogDebug("Entering get service standard history API call");

        try
        {
            _logger.LogInformation($"Getting service standard history by ID='{id}'");

            var history = await _historyPersistence.GetHistoryAsync(id);
            _logger.LogInformation("Found {Count} history entries for standard {StandardId}", history.Count(), id);
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
}


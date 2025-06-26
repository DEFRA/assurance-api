using Asp.Versioning;
using AssuranceApi.Profession.Models;
using AssuranceApi.Profession.Services;
using AssuranceApi.Utils;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApi.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:ApiVersion}/professions")]
public class ProfessionsController : ControllerBase
{
    private readonly IProfessionPersistence _persistence;
    private readonly IProfessionHistoryPersistence _historyPersistence;
    private readonly IValidator<ProfessionModel> _validator;
    private readonly ILogger<ProfessionsController> _logger;

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

    [HttpPost]
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

            return CreatedAtAction(nameof(GetById), new { id = profession.Id }, profession);
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

    [HttpPost("deleteAll")]
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

    [HttpPost("seed")]
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

    [HttpDelete("{id}")]
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

    [HttpPost("{id}/restore")]
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

    [HttpGet]
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

    [HttpGet("{id}")]
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

    [HttpGet("{professionId}/history")]
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
}

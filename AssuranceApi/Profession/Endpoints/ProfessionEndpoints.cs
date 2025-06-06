using AssuranceApi.Profession.Models;
using AssuranceApi.Profession.Services;
using AssuranceApi.Profession.Validators;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace AssuranceApi.Profession.Endpoints;

public static class ProfessionEndpoints
{
    public static void UseProfessionEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("/professions", async (
            [FromBody] ProfessionModel profession,
            IProfessionPersistence persistence,
            IValidator<ProfessionModel> validator
        ) => await Create(profession, persistence, validator)).RequireAuthorization("RequireAuthenticated");
        
        app.MapPost("/professions/deleteAll", async (IProfessionPersistence persistence) =>
        {
            try
            {
                await persistence.DeleteAllAsync();
                return Results.Ok("All professions deleted");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to delete professions: {ex.Message}");
            }
        }).RequireAuthorization("RequireAuthenticated");

        app.MapPost("/professions/seed", async (
            [FromBody] ProfessionModel[] professions,
            IProfessionPersistence persistence,
            IValidator<ProfessionModel> validator,
            ILogger<string> logger
        ) => {
            try
            {
                logger.LogInformation("Seeding {Count} professions", professions.Length);
                
                // Validate each profession
                var validationErrors = new List<string>();
                foreach (var profession in professions)
                {
                    var validationResult = await validator.ValidateAsync(profession);
                    if (!validationResult.IsValid)
                    {
                        validationErrors.AddRange(validationResult.Errors.Select(e => e.ErrorMessage));
                    }
                }
                
                if (validationErrors.Any())
                {
                    return Results.BadRequest(new { Errors = validationErrors });
                }
                
                // Create each profession
                var createdCount = 0;
                foreach (var profession in professions)
                {
                    var created = await persistence.CreateAsync(profession);
                    if (created)
                    {
                        createdCount++;
                    }
                }
                
                logger.LogInformation("Successfully seeded {CreatedCount} out of {TotalCount} professions", 
                    createdCount, professions.Length);
                
                return Results.Ok(new { Message = $"Seeded {createdCount} professions successfully" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to seed professions");
                return Results.Problem($"Failed to seed professions: {ex.Message}");
            }
        }).RequireAuthorization("RequireAuthenticated");

        app.MapDelete("/professions/{id}", async (
            string id,
            IProfessionPersistence persistence) =>
        {
            var success = await persistence.SoftDeleteAsync(id, "System");
            return success ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("RequireAuthenticated");

        app.MapPost("/professions/{id}/restore", async (
            string id,
            IProfessionPersistence persistence) =>
        {
            var success = await persistence.RestoreAsync(id);
            return success ? Results.Ok() : Results.NotFound();
        }).RequireAuthorization("RequireAuthenticated");

        // Read-only endpoints without authentication
        app.MapGet("/professions", GetAll);
        app.MapGet("/professions/{id}", GetById);
        app.MapGet("/professions/{professionId}/history", async (
            string professionId,
            IProfessionHistoryPersistence historyPersistence,
            ILogger<string> logger) =>
        {
            logger.LogInformation("Fetching history for profession {ProfessionId}", professionId);
            var history = await historyPersistence.GetHistoryAsync(professionId);
            logger.LogInformation("Found {Count} history entries for profession {ProfessionId}", 
                history.Count(), professionId);
            return Results.Ok(history);
        });
    }

    private static async Task<IResult> Create(
        ProfessionModel profession,
        IProfessionPersistence persistence,
        IValidator<ProfessionModel> validator)
    {
        var validationResult = await validator.ValidateAsync(profession);
        if (!validationResult.IsValid)
        {
            return Results.BadRequest(validationResult.Errors);
        }

        var created = await persistence.CreateAsync(profession);
        if (!created)
        {
            return Results.BadRequest("Failed to create profession");
        }

        return Results.Created($"/professions/{profession.Id}", profession);
    }

    private static async Task<IResult> GetAll(
        IProfessionPersistence persistence,
        bool includeInactive = false)
    {
        var professions = includeInactive
            ? await persistence.GetAllAsync()
            : await persistence.GetAllActiveAsync();
        return Results.Ok(professions);
    }

    private static async Task<IResult> GetById(
        string id, 
        IProfessionPersistence persistence,
        bool includeInactive = false)
    {
        var profession = includeInactive
            ? await persistence.GetByIdAsync(id)
            : await persistence.GetActiveByIdAsync(id);
        return profession is not null ? Results.Ok(profession) : Results.NotFound();
    }
}
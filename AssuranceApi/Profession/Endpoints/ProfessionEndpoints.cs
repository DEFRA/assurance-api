using AssuranceApi.Profession.Models;
using AssuranceApi.Profession.Services;
using AssuranceApi.Profession.Validators;
using Microsoft.AspNetCore.Authorization;
using FluentValidation;

namespace AssuranceApi.Profession.Endpoints;

public static class ProfessionEndpoints
{
    public static void UseProfessionEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("/professions", Create).RequireAuthorization("RequireAuthenticated");
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

    private static async Task<IResult> GetAll(IProfessionPersistence persistence)
    {
        var professions = await persistence.GetAllAsync();
        return Results.Ok(professions);
    }

    private static async Task<IResult> GetById(string id, IProfessionPersistence persistence)
    {
        var profession = await persistence.GetByIdAsync(id);
        return profession is not null ? Results.Ok(profession) : Results.NotFound();
    }
} 
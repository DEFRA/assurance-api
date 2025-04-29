using AssuranceApi.ServiceStandard.Models;
using AssuranceApi.ServiceStandard.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace AssuranceApi.ServiceStandard.Endpoints;

public static class ServiceStandardEndpoints
{
    public static void UseServiceStandardEndpoints(this IEndpointRouteBuilder app)
    {
        // Protected endpoints that require authentication
        app.MapPost("serviceStandards/seed", SeedStandards).RequireAuthorization("RequireAuthenticated");
        app.MapPost("/serviceStandards/deleteAll", async (IServiceStandardPersistence persistence) =>
        {
            try
            {
                await persistence.DeleteAllAsync();
                return Results.Ok("All service standards deleted");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to delete service standards: {ex.Message}");
            }
        }).RequireAuthorization("RequireAuthenticated");

        // Read-only endpoints without authentication
        app.MapGet("serviceStandards", GetAll);
        app.MapGet("serviceStandards/{id}", GetById);
        app.MapGet("/serviceStandards/{standardId}/history", async (
            string standardId,
            IServiceStandardHistoryPersistence historyPersistence,
            ILogger<string> logger) =>
        {
            logger.LogInformation("Fetching history for standard {StandardId}", standardId);
            var history = await historyPersistence.GetHistoryAsync(standardId);
            logger.LogInformation("Found {Count} history entries for standard {StandardId}", 
                history.Count(), standardId);
            return Results.Ok(history);
        });
    }

    private static async Task<IResult> SeedStandards(
        List<ServiceStandardModel> standards, 
        IServiceStandardPersistence persistence)
    {
        if (!standards.Any())
        {
            await persistence.DeleteAllAsync();
            return Results.Ok();
        }

        var created = await persistence.SeedStandardsAsync(standards);
        return created ? Results.Ok() : Results.BadRequest("Failed to seed standards");
    }

    private static async Task<IResult> GetAll(IServiceStandardPersistence persistence)
    {
        var standards = await persistence.GetAllAsync();
        return Results.Ok(standards);
    }

    private static async Task<IResult> GetById(string id, IServiceStandardPersistence persistence)
    {
        var standard = await persistence.GetByIdAsync(id);
        return standard is not null ? Results.Ok(standard) : Results.NotFound();
    }

    private static List<ServiceStandardModel> GetDefaultStandards()
    {
        return new List<ServiceStandardModel>
        {
            new() { Number = 1, Name = "Understand users and their needs", Description = "Research to develop a deep knowledge of who the service users are and what that means for digital service design" },
            new() { Number = 2, Name = "Solve a whole problem for users", Description = "Work to solve a whole problem for users to achieve a goal that delivers a complete end to end user journey" },
            // ... add all standards
        };
    }
} 
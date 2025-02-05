using AssuranceApi.Project.Models;
using AssuranceApi.Project.Services;
using FluentValidation;

namespace AssuranceApi.Project.Endpoints;

public static class ProjectEndpoints
{
    public static void UseProjectEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("projects", Create);
        app.MapGet("projects", GetAll);
        app.MapGet("projects/{id}", GetById);
        app.MapPut("projects/{id}", Update);
    }

    private static async Task<IResult> Create(
        ProjectModel project, 
        IProjectPersistence persistence,
        IValidator<ProjectModel> validator)
    {
        var validationResult = await validator.ValidateAsync(project);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var created = await persistence.CreateAsync(project);
        if (!created) return Results.BadRequest("Failed to create project");

        return Results.Created($"/projects/{project.Id}", project);
    }

    private static async Task<IResult> GetAll(IProjectPersistence persistence)
    {
        var projects = await persistence.GetAllAsync();
        return Results.Ok(projects);
    }

    private static async Task<IResult> GetById(string id, IProjectPersistence persistence)
    {
        var project = await persistence.GetByIdAsync(id);
        return project is not null ? Results.Ok(project) : Results.NotFound();
    }

    private static async Task<IResult> Update(
        string id,
        ProjectModel project,
        IProjectPersistence persistence,
        IValidator<ProjectModel> validator)
    {
        project.Id = id;
        var validationResult = await validator.ValidateAsync(project);
        if (!validationResult.IsValid) return Results.BadRequest(validationResult.Errors);

        var updated = await persistence.UpdateAsync(project);
        return updated ? Results.Ok(project) : Results.NotFound();
    }
} 
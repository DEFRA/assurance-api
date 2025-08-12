using AssuranceApi.Data;
using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Validators;

/// <summary>
/// Validator for the <see cref="ProjectModel"/> class.
/// Ensures that the properties of a project meet the required validation rules.
/// </summary>
public class ProjectValidator : AbstractValidator<ProjectModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectValidator"/> class.
    /// </summary>
    public ProjectValidator()
    {
        // For updates, we need to be more lenient as fields might be partial
        RuleFor(x => x.Name).NotEmpty().When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Status)
            .Must(status => !string.IsNullOrEmpty(status) && Enum.TryParse<ProjectRatings>(status, true, out _))
            .WithMessage("Status must be a valid ProjectRatings value.");

        RuleFor(x => x.Commentary).NotNull();
        RuleFor(x => x.Phase)
            .Must(x => string.IsNullOrEmpty(x) ||
                x == "Discovery"
                || x == "Alpha"
                || x == "Private Beta"
                || x == "Public Beta"
                || x == "Live"
            );
        // DefCode is optional - no validation required
    }
}

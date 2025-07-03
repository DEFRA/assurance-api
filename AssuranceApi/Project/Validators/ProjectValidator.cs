using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Project.Validators;

public class ProjectValidator : AbstractValidator<ProjectModel>
{
    public ProjectValidator()
    {
        // For updates, we need to be more lenient as fields might be partial
        RuleFor(x => x.Name).NotEmpty().When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Status)
            .Must(x => string.IsNullOrEmpty(x) || 
                x == "RED"
                || x == "AMBER_RED"
                || x == "AMBER"
                || x == "GREEN_AMBER"
                || x == "GREEN"
                || x == "TBC"
            );
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

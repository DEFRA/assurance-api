using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Project.Validators;

public class ProjectValidator : AbstractValidator<ProjectModel>
{
    public ProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x =>
                x == "RED"
                || x == "AMBER_RED"
                || x == "AMBER"
                || x == "GREEN_AMBER"
                || x == "GREEN"
                || x == "TBC"
            );
        RuleFor(x => x.Commentary).NotNull();
        RuleFor(x => x.Phase)
            .NotEmpty()
            .Must(x =>
                x == "Discovery"
                || x == "Alpha"
                || x == "Private Beta"
                || x == "Public Beta"
                || x == "Live"
            );
        // DefCode is optional - no validation required
    }
}

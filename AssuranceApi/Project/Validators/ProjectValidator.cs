using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Project.Validators;

public class ProjectValidator : AbstractValidator<ProjectModel>
{
    public ProjectValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(x => 
            x == "RED" || x == "AMBER" || x == "GREEN");
        RuleFor(x => x.Commentary).NotEmpty();
        RuleFor(x => x.LastUpdated).NotEmpty();
        RuleFor(x => x.Standards).NotNull();
        RuleForEach(x => x.Standards).SetValidator(new StandardValidator());
    }
}

public class StandardValidator : AbstractValidator<StandardModel>
{
    public StandardValidator()
    {
        RuleFor(x => x.StandardId).NotEmpty();
        RuleFor(x => x.Status).NotEmpty().Must(x => 
            x == "RED" || x == "AMBER" || x == "GREEN");
        RuleFor(x => x.Commentary).NotEmpty();
    }
} 
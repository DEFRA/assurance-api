using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Project.Validators;

public class ProjectValidator : AbstractValidator<ProjectModel>
{
    public ProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x => new[] { "RED", "AMBER", "GREEN" }.Contains(x))
            .WithMessage("Status must be RED, AMBER, or GREEN");

        RuleFor(x => x.LastUpdated)
            .NotEmpty();

        RuleFor(x => x.Commentary)
            .NotEmpty()
            .MaximumLength(2000);

        RuleForEach(x => x.Standards).SetValidator(new StandardAssessmentValidator());
    }
}

public class StandardAssessmentValidator : AbstractValidator<StandardAssessment>
{
    public StandardAssessmentValidator()
    {
        RuleFor(x => x.StandardId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x => new[] { "RED", "AMBER", "GREEN" }.Contains(x))
            .WithMessage("Status must be RED, AMBER, or GREEN");

        RuleFor(x => x.Commentary)
            .NotEmpty()
            .MaximumLength(1000);
    }
} 
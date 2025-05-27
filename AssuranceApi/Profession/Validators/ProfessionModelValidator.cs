using FluentValidation;
using AssuranceApi.Profession.Models;

namespace AssuranceApi.Profession.Validators;

public class ProfessionModelValidator : AbstractValidator<ProfessionModel>
{
    public ProfessionModelValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Id must contain only lowercase letters, numbers, and hyphens");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[A-Za-z0-9\\s-]+$")
            .WithMessage("Name must contain only letters, numbers, spaces, and hyphens");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);
    }
} 
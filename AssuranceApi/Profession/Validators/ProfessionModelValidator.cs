using AssuranceApi.Profession.Models;
using FluentValidation;

namespace AssuranceApi.Profession.Validators;

/// <summary>
/// Validator for the <see cref="ProfessionModel"/> class.
/// Ensures that the properties of the model meet the required validation rules.
/// </summary>
public class ProfessionModelValidator : AbstractValidator<ProfessionModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProfessionModelValidator"/> class.
    /// Defines validation rules for the <see cref="ProfessionModel"/> properties.
    /// </summary>
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

        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
    }
}

using AssuranceApi.Data.Models;
using FluentValidation;

namespace AssuranceApi.Validators;

/// <summary>
/// Validator for the <see cref="ThemeModel"/> class.
/// Ensures that the properties of the model meet the required validation rules.
/// </summary>
public class ThemeValidator : AbstractValidator<ThemeModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeValidator"/> class.
    /// Defines validation rules for the <see cref="ThemeModel"/>.
    /// </summary>
    public ThemeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200)
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required.")
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters.");
    }
}


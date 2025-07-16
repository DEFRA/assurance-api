using AssuranceApi.ServiceStandard.Models;
using FluentValidation;

namespace AssuranceApi.Validators;

/// <summary>
/// Validator for the <see cref="ServiceStandardModel"/> class.
/// Ensures that the properties of the model meet the defined validation rules.
/// </summary>
public class ServiceStandardValidator : AbstractValidator<ServiceStandardModel>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceStandardValidator"/> class.
    /// Defines validation rules for the <see cref="ServiceStandardModel"/>.
    /// </summary>
    public ServiceStandardValidator()
    {
        RuleFor(x => x.Number).GreaterThan(0).LessThanOrEqualTo(20);

        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);

        RuleFor(x => x.Description).NotEmpty().MaximumLength(1000);

        RuleFor(x => x.Guidance).NotEmpty().MaximumLength(3000);
    }
}

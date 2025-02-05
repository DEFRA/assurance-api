using AssuranceApi.ServiceStandard.Models;
using FluentValidation;

namespace AssuranceApi.ServiceStandard.Validators;

public class ServiceStandardValidator : AbstractValidator<ServiceStandardModel>
{
    public ServiceStandardValidator()
    {
        RuleFor(x => x.Number)
            .GreaterThan(0)
            .LessThanOrEqualTo(14);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);
    }
} 
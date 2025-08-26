using AssuranceApi.Data.Models;
using FluentValidation;

namespace AssuranceApi.Validators
{
    /// <summary>
    /// Validator for the <see cref="DeliveryGroupModel"/> class.
    /// Ensures that the properties of the model meet the required validation rules.
    /// </summary>
    public class DeliveryGroupValidator : AbstractValidator<DeliveryGroupModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryGroupValidator"/> class.
        /// Defines validation rules for the <see cref="DeliveryGroupModel"/>.
        /// </summary>
        public DeliveryGroupValidator()
        {
            RuleFor(x => x.Name).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage("Name must be a valid string.");

            RuleFor(x => x.Status).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage("Status must be a valid string.");

            // Lead is optional - no validation required
        }
    }
}

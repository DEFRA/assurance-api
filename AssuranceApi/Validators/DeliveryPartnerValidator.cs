using AssuranceApi.Data.Models;
using FluentValidation;

namespace AssuranceApi.Validators
{
    /// <summary>
    /// Validator for the <see cref="DeliveryPartnerModel"/> class.
    /// Ensures that the properties of the model meet the required validation rules.
    /// </summary>
    public class DeliveryPartnerValidator : AbstractValidator<DeliveryPartnerModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeliveryPartnerValidator"/> class.
        /// Defines validation rules for the <see cref="DeliveryPartnerModel"/>.
        /// </summary>
        public DeliveryPartnerValidator()
        {
            RuleFor(x => x.Name).Must(x => !string.IsNullOrEmpty(x))
                .WithMessage("Name must be a valid string.");
        }
    }
}

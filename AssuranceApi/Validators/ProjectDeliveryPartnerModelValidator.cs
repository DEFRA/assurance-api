using AssuranceApi.Data.Models;
using FluentValidation;

namespace AssuranceApi.Validators
{
    /// <summary>
    /// Validator for the <see cref="ProjectDeliveryPartnerModel"/> class.
    /// Ensures that the ProjectId and DeliveryPartnerId properties are not null or empty.
    /// </summary>
    public class ProjectDeliveryPartnerModelValidator : AbstractValidator<ProjectDeliveryPartnerModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectDeliveryPartnerModelValidator"/> class.
        /// </summary>
        public ProjectDeliveryPartnerModelValidator()
        {
            RuleFor(x => x.ProjectId)
                .NotEmpty()
                .WithMessage("Project ID cannot be null or empty.");
            RuleFor(x => x.DeliveryPartnerId)
                .NotEmpty()
                .WithMessage("Delivery Partner ID cannot be null or empty.");
        }
    }
}

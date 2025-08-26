using AssuranceApi.Data.Models;
using AssuranceApi.Project.Models;
using FluentValidation;

namespace AssuranceApi.Validators
{
    public class ProjectDeliveryPartnerModelValidator : AbstractValidator<ProjectDeliveryPartnerModel>
    {
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

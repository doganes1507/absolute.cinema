using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class UpdateEmailAddressDtoValidator : AbstractValidator<UpdateEmailAddressDto>
{
    public UpdateEmailAddressDtoValidator()
    {
        RuleFor(x => x.NewEmailAddress).SetValidator(new UserEmailAddressValidator());
    }
}
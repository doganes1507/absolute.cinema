using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class ConfirmCodeDtoValidator : AbstractValidator<ConfirmCodeDto>
{
    public ConfirmCodeDtoValidator()
    {
        RuleFor(x => x.EmailAddress).SetValidator(new UserEmailAddressValidator());
    }
}
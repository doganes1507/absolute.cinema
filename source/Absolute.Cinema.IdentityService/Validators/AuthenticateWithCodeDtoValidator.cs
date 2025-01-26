using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class AuthenticateWithCodeDtoValidator : AbstractValidator<AuthenticateWithCodeDto>
{
    public AuthenticateWithCodeDtoValidator()
    {
        RuleFor(x => x.EmailAddress).SetValidator(new UserEmailAddressValidator());
    }
}
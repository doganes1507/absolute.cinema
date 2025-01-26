using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class AuthenticateWithPasswordDtoValidator : AbstractValidator<AuthenticateWithPasswordDto>
{
    public AuthenticateWithPasswordDtoValidator()
    {
        RuleFor(x => x.EmailAddress).SetValidator(new UserEmailAddressValidator());
        RuleFor(x => x.Password).SetValidator(new UserPasswordValidator());
    }
}
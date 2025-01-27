using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators.AdminController;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.EmailAdress)
            .SetValidator(new UserEmailAddressValidator());

        RuleFor(x => x.Password)
            .SetValidator(new UserPasswordValidator());
        //.When(x => !string.IsNullOrEmpty(x.Password));
    }
}
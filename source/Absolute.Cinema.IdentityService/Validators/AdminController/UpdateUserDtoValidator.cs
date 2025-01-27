using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators.AdminController;

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.NewEmailAddress)
            .SetValidator(new UserEmailAddressValidator())
            .When(x => !string.IsNullOrEmpty(x.NewEmailAddress));
        
        RuleFor(x => x.NewPassword)
            .SetValidator(new UserPasswordValidator())
            .When(x => !string.IsNullOrEmpty(x.NewPassword));
    }
}
using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class UpdatePasswordDtoValidator : AbstractValidator<UpdatePasswordDto>
{
    public UpdatePasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword).SetValidator(new UserPasswordValidator());
    }
}
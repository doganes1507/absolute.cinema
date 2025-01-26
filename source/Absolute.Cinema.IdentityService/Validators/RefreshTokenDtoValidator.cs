using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.UserId).SetValidator(new GuidValidator());
    }
}
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class UserEmailAddressValidator : AbstractValidator<string>
{
    public UserEmailAddressValidator()
    {
        RuleFor(email => email)
            .NotEmpty().WithMessage("Email is required");
    }
}
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators.PropertyValidators;

public class UserEmailAddressValidator : AbstractValidator<string>
{
    public UserEmailAddressValidator()
    {
        RuleFor(email => email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email address must be a valid email address.")
            .MaximumLength(64).WithMessage("Email address must be less than 64 characters.");
    }
}
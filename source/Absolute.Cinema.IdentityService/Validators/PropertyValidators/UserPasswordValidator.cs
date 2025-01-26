using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators.PropertyValidators;

public class UserPasswordValidator : AbstractValidator<string>
{
    public UserPasswordValidator()
    {
        RuleFor(password => password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .Matches(@"^[a-zA-Z0-9]*$").WithMessage("Password must contain only Latin letters and digits.")
            .Matches(@"^[\S]+$").WithMessage("Password cannot contain spaces.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}
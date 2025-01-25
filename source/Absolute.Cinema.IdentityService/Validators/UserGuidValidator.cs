using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class UserGuidValidator : AbstractValidator<string>
{
    public UserGuidValidator()
    {
        RuleFor(userId => userId)
            .NotEmpty().WithMessage("UserId is required")
            .Must(BeValidGuid).WithMessage("UserId must be a valid GUID");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}
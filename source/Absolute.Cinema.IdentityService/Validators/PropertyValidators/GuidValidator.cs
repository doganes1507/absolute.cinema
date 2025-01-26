using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators.PropertyValidators;

public class GuidValidator : AbstractValidator<string>
{
    public GuidValidator()
    {
        RuleFor(userId => userId)
            .NotEmpty().WithMessage("Id is required")
            .Must(BeValidGuid).WithMessage("Id must be a valid GUID");
    }

    private bool BeValidGuid(string id)
    {
        return Guid.TryParse(id, out _);
    }
}
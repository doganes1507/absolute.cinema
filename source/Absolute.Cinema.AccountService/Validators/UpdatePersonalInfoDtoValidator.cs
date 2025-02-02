using Absolute.Cinema.AccountService.DataObjects;
using FluentValidation;

namespace Absolute.Cinema.AccountService.Validators;

public class UpdatePersonalInfoDtoValidator : AbstractValidator<UpdatePersonalInfoDto>
{
    public UpdatePersonalInfoDtoValidator()
    {
        RuleFor(x => x.DateOfBirth)
            .Must(BeInThePast).When(x => x.DateOfBirth.HasValue)
            .WithMessage("The date of birth must be in the past.");
    }

    private bool BeInThePast(DateOnly? arg)
    {
        var dateOfBirth = arg!.Value;
        var today = DateOnly.FromDateTime(DateTime.Today);

        return dateOfBirth <= today;
    }
}
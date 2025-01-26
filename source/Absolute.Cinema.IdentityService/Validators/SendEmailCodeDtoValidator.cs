using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Validators.PropertyValidators;
using FluentValidation;

namespace Absolute.Cinema.IdentityService.Validators;

public class SendEmailCodeDtoValidator : AbstractValidator<SendEmailCodeDto>
{
    public SendEmailCodeDtoValidator()
    {
        RuleFor(x => x.EmailAddress).SetValidator(new UserEmailAddressValidator());
    }
}
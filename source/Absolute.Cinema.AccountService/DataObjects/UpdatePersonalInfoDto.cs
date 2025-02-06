using Absolute.Cinema.AccountService.Models.Enumerations;

namespace Absolute.Cinema.AccountService.DataObjects;

public class UpdatePersonalInfoDto
{
    public string? FirstName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
}
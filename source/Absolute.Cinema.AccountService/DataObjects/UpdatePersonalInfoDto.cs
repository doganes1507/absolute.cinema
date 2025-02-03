namespace Absolute.Cinema.AccountService.DataObjects;

public class UpdatePersonalInfoDto
{
    public string? FirstName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public bool? Gender { get; set; }
}
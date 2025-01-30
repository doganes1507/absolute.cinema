namespace Absolute.Cinema.AccountService.DataObjects;

public class UpdatePersonalInfoDto
{
    public string? FirstName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool? Gender { get; set; }
}
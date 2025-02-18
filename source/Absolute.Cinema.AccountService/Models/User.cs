using Absolute.Cinema.AccountService.Models.Enumerations;

namespace Absolute.Cinema.AccountService.Models;

public class User
{
    public Guid Id { get; set; }
    public string EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public DateTime RegistrationDateTime { get; set; } = DateTime.UtcNow;
}
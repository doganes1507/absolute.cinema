namespace Absolute.Cinema.AccountService.Models;

public class User
{
    public Guid Id { get; set; }
    public string EmailAddress { get; set; }
    public string? FirstName { get; set; }
    public DateOnly? BirthDate { get; set; }
    public bool Gender { get; set; }
}
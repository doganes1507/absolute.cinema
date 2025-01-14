namespace Absolute.Cinema.IdentityService.Models;

public class User
{
    public Guid Id { get; set; }
    public string EmailAddress { get; set; }
    public string? HashPassword { get; set; }
}
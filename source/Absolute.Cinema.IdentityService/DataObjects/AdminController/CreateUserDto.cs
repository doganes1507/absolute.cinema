namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class CreateUserDto
{
    public string EmailAddress { get; set; }
    public string? Password { get; set; }
    public string Role { get; set; }
}
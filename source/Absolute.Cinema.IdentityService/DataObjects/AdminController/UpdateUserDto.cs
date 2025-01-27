namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class UpdateUserDto
{
    public string? NewEmailAddress { get; set; }
    public string? NewPassword { get; set; }
    public string? NewRole { get; set; }
}
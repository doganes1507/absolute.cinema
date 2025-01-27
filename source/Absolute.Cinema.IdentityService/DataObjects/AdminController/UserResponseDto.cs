using Absolute.Cinema.IdentityService.Models;

namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class UserResponseDto(User user)
{
    public string UserId { get; set; } = user.Id.ToString();
    public string EmailAddress { get; set; } = user.EmailAddress;
    public string Role { get; set; } = user.Role.Name;
}
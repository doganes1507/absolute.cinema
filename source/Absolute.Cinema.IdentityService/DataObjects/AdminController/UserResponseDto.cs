using System.ComponentModel.DataAnnotations;
using Absolute.Cinema.IdentityService.Models;

namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class UserResponseDto()
{
    public string? UserId { get; set; }
    public string? EmailAddress { get; set; }
    public string? Role { get; set; }

    public static UserResponseDto FormDto(User user)
    {
        return new UserResponseDto
        {
            UserId = user.Id.ToString(),
            EmailAddress = user.EmailAddress,
            Role = user.Role.Name
        };
    }
}
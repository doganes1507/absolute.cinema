using Absolute.Cinema.IdentityService.Models;

namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class RoleResponseDto(Role role)
{
    public string RoleId { get; set; } = role.Id.ToString();
    public string RoleName { get; set; } = role.Name;
}
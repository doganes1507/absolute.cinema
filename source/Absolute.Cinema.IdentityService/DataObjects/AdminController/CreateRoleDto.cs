using HostingEnvironmentExtensions = Microsoft.AspNetCore.Hosting.HostingEnvironmentExtensions;

namespace Absolute.Cinema.IdentityService.DataObjects.AdminController;

public class CreateRoleDto
{
    public string RoleName { get; set; }
}
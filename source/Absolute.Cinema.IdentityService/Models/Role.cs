namespace Absolute.Cinema.IdentityService.Models;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    
    public virtual List<User> Users { get; set; }
}
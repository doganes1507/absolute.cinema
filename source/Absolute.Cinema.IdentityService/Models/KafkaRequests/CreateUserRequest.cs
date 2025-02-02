using Absolute.Cinema.IdentityService.DataObjects.AdminController;

namespace Absolute.Cinema.IdentityService.Models.KafkaRequests;

public class CreateUserRequest(User? user)
{
    public Guid UserId { get; set; } = user.Id;
    public string EmailAddress { get; set; } = user.EmailAddress;
}
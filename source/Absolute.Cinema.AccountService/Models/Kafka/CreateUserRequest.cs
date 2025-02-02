namespace Absolute.Cinema.AccountService.Models.Kafka;

public class CreateUserRequest()
{
    public string UserId { get; set; }
    public string EmailAddress { get; set; }
}
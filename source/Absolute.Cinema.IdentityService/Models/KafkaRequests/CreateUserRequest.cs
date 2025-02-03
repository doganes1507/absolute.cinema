namespace Absolute.Cinema.IdentityService.Models.KafkaRequests;

public record CreateUserRequest(Guid UserId, string EmailAddress);
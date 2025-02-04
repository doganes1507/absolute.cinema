namespace Absolute.Cinema.IdentityService.Models.KafkaRequests;

public record SyncUserRequest(Guid UserId, string EmailAddress);
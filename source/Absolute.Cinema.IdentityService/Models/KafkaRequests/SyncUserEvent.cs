namespace Absolute.Cinema.IdentityService.Models.KafkaRequests;

public record SyncUserEvent(Guid UserId, string EmailAddress);
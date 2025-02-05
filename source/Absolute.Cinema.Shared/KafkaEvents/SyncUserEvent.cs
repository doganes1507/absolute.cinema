namespace Absolute.Cinema.Shared.KafkaEvents;

public record SyncUserEvent(Guid UserId, string EmailAddress);
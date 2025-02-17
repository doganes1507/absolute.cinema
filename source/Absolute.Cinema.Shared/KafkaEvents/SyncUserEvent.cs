using Absolute.Cinema.Shared.Models.Enumerations;

namespace Absolute.Cinema.Shared.KafkaEvents;

public record SyncUserEvent(Guid UserId, string EmailAddress, DbOperation Operation);
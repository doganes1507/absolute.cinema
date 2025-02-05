namespace Absolute.Cinema.AccountService.Models;

public record SyncUserEvent(Guid UserId, string EmailAddress);
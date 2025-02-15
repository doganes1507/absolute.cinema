using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.Models;
using Absolute.Cinema.AccountService.Models.Enumerations;
using Absolute.Cinema.Shared.Interfaces;
using Absolute.Cinema.Shared.KafkaEvents;
using Absolute.Cinema.Shared.Models.Enumerations;
using KafkaFlow;

namespace Absolute.Cinema.AccountService.Handlers;

public class SyncUserHandler(ApplicationDbContext dbContext, ICacheService cacheService, IConfiguration configuration) : IMessageHandler<SyncUserEvent>
{
    public async Task Handle(IMessageContext context, SyncUserEvent message)
    {
        var user = await dbContext.Users.FindAsync(message.UserId);

        switch (message.Operation)
        {
            case DbOperation.Create:
                await HandleCreate(message);
                break;
            case DbOperation.Update:
                await HandleUpdate(message);
                break;
            case DbOperation.Delete:
                await HandleDelete(message);
                break;
        }
        
        await dbContext.SaveChangesAsync();
    }

    private User CreateUserFromMessage(SyncUserEvent message)
    {
        var user  = new User
        {
            Id = message.UserId,
            EmailAddress = message.EmailAddress,
            Gender = Gender.Unspecified,
            RegistrationDateTime = DateTime.UtcNow,
        };

        return user;
    }

    private async Task HandleCreate(SyncUserEvent message)
    {
        var user = CreateUserFromMessage(message);
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        if (cacheService.IsConnected())
        {
            var expiry = TimeSpan.FromMinutes(configuration.GetValue<int>("Redis:UserCacheTimeMinutes"));
            await cacheService.SetAsync(user.Id.ToString(), user, expiry);
        }
    }

    private async Task HandleUpdate(SyncUserEvent message)
    {
        var user = await dbContext.Users.FindAsync(message.UserId);

        if (user == null)
        {
            user = CreateUserFromMessage(message);
            await dbContext.Users.AddAsync(user);
        }
        else
        {
            user.EmailAddress = message.EmailAddress;
        }
        
        await dbContext.SaveChangesAsync();
        
        if (cacheService.IsConnected())
        {
            var expiry = TimeSpan.FromMinutes(configuration.GetValue<int>("Redis:UserCacheTimeMinutes"));
            await cacheService.SetAsync(user.Id.ToString(), user, expiry);
        }
    }

    private async Task HandleDelete(SyncUserEvent message)
    {
        var user = await dbContext.Users.FindAsync(message.UserId);
        if (user != null)
            dbContext.Users.Remove(user);
        
        await dbContext.SaveChangesAsync();
        
        if (cacheService.IsConnected())
            await cacheService.DeleteAsync(message.UserId.ToString());
    }
}
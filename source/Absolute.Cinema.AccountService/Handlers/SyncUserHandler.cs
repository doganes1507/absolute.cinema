using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.Models;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models.KafkaRequests;
using KafkaFlow;

namespace Absolute.Cinema.AccountService.Handlers;

public class SyncUserHandler(ApplicationDbContext dbContext, ICacheService cacheService, IConfiguration configuration) : IMessageHandler<SyncUserRequest>
{
    public async Task Handle(IMessageContext context, SyncUserRequest message)
    {
        var user = await dbContext.Users.FindAsync(message.UserId);  
        
        if (user == null)
        {
            user = new User
            {
                Id = message.UserId,
                EmailAddress = message.EmailAddress,
                RegistrationDateTime = DateTime.UtcNow,
            };
            
            await dbContext.Users.AddAsync(user);
        }
        else
        {
            user.EmailAddress = message.EmailAddress;
        }
        
        if (cacheService.IsConnected())
        {
            var expiry = TimeSpan.FromMinutes(configuration.GetValue<int>("Redis:UserCacheTimeMinutes"));
            await cacheService.SetAsync(user.Id.ToString(), user, expiry);
        }
        
        await dbContext.SaveChangesAsync();
    }
}
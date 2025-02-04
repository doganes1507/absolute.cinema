using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.Models;
using Absolute.Cinema.IdentityService.Models.KafkaRequests;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.AccountService.Handlers;

public class CreateUserHandler(ApplicationDbContext dbContext) : IMessageHandler<CreateUserRequest>
{
    public async Task Handle(IMessageContext context, CreateUserRequest message)
    {
        if ( !await dbContext.Users.AnyAsync(u => u.EmailAddress == message.EmailAddress))
        {
            await dbContext.Users.AddAsync(new User
            {
                Id = message.UserId,
                EmailAddress = message.EmailAddress,
                RegistrationDateTime = DateTime.UtcNow,
            });
                
            await dbContext.SaveChangesAsync();
        }
    }
}
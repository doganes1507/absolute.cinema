using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.Models;
using Absolute.Cinema.IdentityService.Models.KafkaRequests;
using KafkaFlow;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.AccountService.Handlers;

public class CreateUserHandler : IMessageHandler<CreateUserRequest>
{
    private readonly ApplicationDbContext _dbContext;
    
    public CreateUserHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task Handle(IMessageContext context, CreateUserRequest message)
    {
        if ( !await _dbContext.Users.AnyAsync(u => u.EmailAddress == message.EmailAddress))
        {
            await _dbContext.Users.AddAsync(new User
            {
                Id = message.UserId,
                EmailAddress = message.EmailAddress,
                RegistrationDateTime = DateTime.UtcNow,
            });
                
            await _dbContext.SaveChangesAsync();
        }
        
        // return Task.CompletedTask;
    }
}
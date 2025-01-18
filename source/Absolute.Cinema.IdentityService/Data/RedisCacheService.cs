using StackExchange.Redis;

namespace Absolute.Cinema.IdentityService.Data;

public class RedisCacheService
{
    public readonly IDatabase ConfirmationCodesDb;
    public readonly IDatabase RefreshTokensDb;
    
    public RedisCacheService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        
        var confirmationCodesDatabaseId = configuration.GetValue<int>("Redis:ConfirmationCodeDatabaseId");
        var refreshTokensDatabaseId = configuration.GetValue<int>("Redis:RefreshTokenDatabaseId");
        
        ConfirmationCodesDb = connectionMultiplexer.GetDatabase(confirmationCodesDatabaseId);
        RefreshTokensDb = connectionMultiplexer.GetDatabase(refreshTokensDatabaseId);
    }
}
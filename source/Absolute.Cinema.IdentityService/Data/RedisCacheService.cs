using StackExchange.Redis;

namespace Absolute.Cinema.IdentityService.Data;

public class RedisCacheService
{
    public readonly IDatabase ConfirmationCodesDb;
    public readonly IDatabase RefreshTokensDb;
    public readonly IDatabase EmailVerificationDb;
    
    public RedisCacheService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        
        var confirmationCodesDatabaseId = configuration.GetValue<int>("Redis:ConfirmationCodeDatabaseId");
        var refreshTokensDatabaseId = configuration.GetValue<int>("Redis:RefreshTokenDatabaseId");
        var emailVerificationDatabaseId = configuration.GetValue<int>("Redis:EmailVerificationDatabaseId");
        
        ConfirmationCodesDb = connectionMultiplexer.GetDatabase(confirmationCodesDatabaseId);
        RefreshTokensDb = connectionMultiplexer.GetDatabase(refreshTokensDatabaseId);
        EmailVerificationDb = connectionMultiplexer.GetDatabase(emailVerificationDatabaseId);
    }
}
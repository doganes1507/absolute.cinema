using StackExchange.Redis;

namespace Absolute.Cinema.IdentityService.Data;

public class RedisCacheService
{
    public readonly IDatabase ConfirmationCodesDb;
    public readonly IDatabase EmailVerificationDb;
    public readonly IDatabase RefreshTokensDb;
    
    public RedisCacheService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");
        var connectionMultiplexer = ConnectionMultiplexer.Connect(connectionString);
        
        var confirmationCodesDatabaseId = configuration.GetValue<int>("Redis:ConfirmationCodeDatabaseId");
        var emailVerificationDatabaseId = configuration.GetValue<int>("Redis:EmailVerificationDatabaseId");
        var refreshTokensDatabaseId = configuration.GetValue<int>("Redis:RefreshTokenDatabaseId");
        
        ConfirmationCodesDb = connectionMultiplexer.GetDatabase(confirmationCodesDatabaseId);
        EmailVerificationDb = connectionMultiplexer.GetDatabase(emailVerificationDatabaseId);
        RefreshTokensDb = connectionMultiplexer.GetDatabase(refreshTokensDatabaseId);
    }
}
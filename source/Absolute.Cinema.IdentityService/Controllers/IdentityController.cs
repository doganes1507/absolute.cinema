using Absolute.Cinema.IdentityService.DataContext;
using Absolute.Cinema.IdentityService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Absolute.Cinema.IdentityService.Controllers;

[ApiController]
[Route("identity-service")]
public class IdentityController : ControllerBase
{
    private readonly DatabaseContext _dbContext;
    private readonly IConnectionMultiplexer _redisConnectionMultiplexer;
    private readonly IEmailService _emailService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConfiguration _configuration;
    
    public IdentityController(
        DatabaseContext dbContext,
        IConnectionMultiplexer connectionMultiplexer,
        IEmailService emailService,
        ITokenProvider tokenProvider,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _redisConnectionMultiplexer = connectionMultiplexer;
        _emailService = emailService;
        _tokenProvider = tokenProvider;
        _configuration = configuration;
    }
    
    [HttpPost("SendEmailCode")]
    public async Task<IActionResult> SendEmailCode()
    {
        throw new NotImplementedException();
    }

    [HttpPost("ConfirmCode")]
    public async Task<IActionResult> ConfirmCode()
    {
        throw new NotImplementedException();
    }

    [HttpPost("AuthenticateWithCode")]
    public async Task<IActionResult> AuthenticateWithCode()
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("AuthenticateWithPassword")]
    public async Task<IActionResult> AuthenticateWithPassword()
    {
        throw new NotImplementedException();
    }

    [HttpPost("UpdateEmailAddress")]
    public async Task<IActionResult> UpdateEmailAddress()
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("UpdatePassword")]
    public async Task<IActionResult> UpdatePassword()
    {
        throw new NotImplementedException();
    }

    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken(string userId, string oldRefreshToken)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound("User not found");
        }
        
        var redis = _redisConnectionMultiplexer.GetDatabase(_configuration.GetValue<int>("Redis:RefreshTokenDatabaseId"));
        if (await redis.StringGetAsync(userId) != oldRefreshToken)
        {
            return BadRequest("Refresh token is expired, invalid, or not found");
        }
        
        var newAccessToken = _tokenProvider.GetAccessToken(user);
        var newRefreshToken = _tokenProvider.GetRefreshToken();
        
        redis.StringSet(userId, newRefreshToken);

        return Ok(new { newAccessToken, newRefreshToken });
    }
}
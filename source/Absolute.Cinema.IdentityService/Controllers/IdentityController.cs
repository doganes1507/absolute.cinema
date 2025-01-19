using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Interfaces;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Absolute.Cinema.IdentityService.Controllers;

[ApiController]
[Route("identity-service")]
public class IdentityController : ControllerBase
{
    private readonly DatabaseContext _dbContext;
    private readonly RedisCacheService _redis;
    private readonly IMailService _mailService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConfiguration _configuration;
    
    public IdentityController(
        DatabaseContext dbContext,
        RedisCacheService redis,
        IMailService mailService,
        ITokenProvider tokenProvider,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _redis = redis;
        _mailService = mailService;
        _tokenProvider = tokenProvider;
        _configuration = configuration;
    }
    
    [HttpPost("SendEmailCode")]
    public async Task<IActionResult> SendEmailCode(string email)
    {
        var rnd = new Random();
        var code = rnd.Next(100000, 999999);
        
        await _redisDatabase.StringSetAsync(email, code.ToString());
        
        var mailData = _mailService.CreateBaseMail(email, code);
        var res = await _mailService.SendMailAsync(mailData);
        
        if (res)
            return Ok("Code was successfully sent");
        
        return BadRequest("Failed to send email code");
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
        
        if (await _redis.RefreshTokensDb.StringGetAsync(userId) != oldRefreshToken)
        {
            return BadRequest("Refresh token is expired, invalid, or not found");
        }
        
        var newAccessToken = _tokenProvider.GetAccessToken(user);
        var newRefreshToken = _tokenProvider.GetRefreshToken();
        
        _redis.RefreshTokensDb.StringSet(userId, newRefreshToken);

        return Ok(new { newAccessToken, newRefreshToken });
    }
}
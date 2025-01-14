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
    private readonly IDatabase _redisDatabase;
    private readonly IEmailService _emailService;
    private readonly ITokenProvider _tokenProvider;
    
    public IdentityController(
        DatabaseContext dbContext,
        IConnectionMultiplexer connectionMultiplexer,
        IEmailService emailService,
        ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _redisDatabase = connectionMultiplexer.GetDatabase();
        _emailService = emailService;
        _tokenProvider = tokenProvider;
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
    public async Task<IActionResult> RefreshToken()
    {
        throw new NotImplementedException();
    }
}
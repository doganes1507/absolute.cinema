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
    private readonly IMailService _mailService;
    //Sprivate readonly ITokenProvider _tokenProvider;
    
    public IdentityController(
        DatabaseContext dbContext,
        IConnectionMultiplexer connectionMultiplexer,
        IMailService mailService //,
        //ITokenProvider
        )
    {
        _dbContext = dbContext;
        _redisDatabase = connectionMultiplexer.GetDatabase();
        _mailService = mailService;
        //_tokenProvider = tokenProvider;
    }
    
    [HttpPost("SendEmailCode")]
    public async Task<IActionResult> SendEmailCode(string email, int code)
    {
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
    public async Task<IActionResult> RefreshToken()
    {
        throw new NotImplementedException();
    }
}
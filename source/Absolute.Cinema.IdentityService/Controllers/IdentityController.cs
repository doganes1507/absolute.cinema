using System.Security.Claims;
using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        
        await _redis.ConfirmationCodesDb.StringSetAsync(email, code,
            TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:ConfirmationCodeExpirationInMinutes")));
        
        var mailData = _mailService.CreateBaseMail(email, code);
        
        if (await _mailService.SendMailAsync(mailData))
            return Ok(new {message = "Code was successfully sent"});
        
        return BadRequest(new {message = "Failed to send email code"});
    }

    [HttpPost("ConfirmCode")]
    public async Task<IActionResult> ConfirmCode(string email, int code)
    {
        if (await _redis.ConfirmationCodesDb.StringGetAsync(email) != code) 
            return BadRequest(new {message = "Code was not confirmed"});
        
        await _redis.EmailVerificationDb.StringSetAsync(email, true);
        await _redis.ConfirmationCodesDb.KeyDeleteAsync(email);
        return Ok(new {message = "Code was confirmed"});
    }

    [HttpPost("AuthenticateWithCode")]
    public async Task<IActionResult> AuthenticateWithCode(string email)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
        var confirmed = await _redis.EmailVerificationDb.StringGetDeleteAsync(email);
        
        if (confirmed != true)
            return BadRequest(new {message = "Email wasn't verified"});
        
        if (user != null)
        {
            return Ok(new
            {
                accessToken = _tokenProvider.GetAccessToken(user),
                refreshToken = _tokenProvider.GetRefreshToken(),
                message = "User successfully logged in"
            });
        }

        user = new User { EmailAddress = email, HashPassword = null };
        
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
        
        // Add user creation request to the message broker queue
        
        return Ok(new 
        {
            accessToken = _tokenProvider.GetAccessToken(user),
            refreshToken = _tokenProvider.GetRefreshToken(),
            message = "User successfully registered"
        });
        
    }

    [HttpPost("AuthenticateWithPassword")]
    public async Task<IActionResult> AuthenticateWithPassword(string email, string password)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.EmailAddress == email);
        if (user == null)
            return BadRequest(new { message = "User doesn’t exists" });
        
        if (!BCrypt.Net.BCrypt.Verify(password, user.HashPassword))
            return Unauthorized(new { message = "Invalid credentials"});

        return Ok(new
        {
            accessToken = _tokenProvider.GetAccessToken(user),
            refreshToken = _tokenProvider.GetRefreshToken(),
            message = "User successfully logged in"
        });
    }

    [Authorize]
    [HttpPost("UpdateEmailAddress")]
    public async Task<IActionResult> UpdateEmailAddress(string newEmailAddress)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (_redis.EmailVerificationDb.StringGetAsync(newEmailAddress).Result != true)
        {
            return BadRequest(new {message = "New Email address was not verified"});
        }
        
        user.EmailAddress = newEmailAddress;
        _dbContext.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new {message = "Email address updated"});
    }

    [Authorize]
    [HttpPost("UpdatePassword")]
    public async Task<IActionResult> UpdatePassword(string newPassword)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _dbContext.Users.FindAsync(userId);
        if (user == null)
            return NotFound("User not found");

        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
        
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
        
        return Ok("Password was successfully updated");
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
            return BadRequest(new {message = "Refresh token is expired, invalid, or not found"});
        }

        var newAccessToken = _tokenProvider.GetAccessToken(user);
        var newRefreshToken = _tokenProvider.GetRefreshToken();

        _redis.RefreshTokensDb.StringSet(userId, newRefreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("RefreshToken:ExpirationInDays")));

        return Ok(new { newAccessToken, newRefreshToken });
    }
}
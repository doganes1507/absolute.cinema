using System.Security.Claims;
using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Absolute.Cinema.IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Role = Absolute.Cinema.IdentityService.Models.Role;

namespace Absolute.Cinema.IdentityService.Controllers;

[ApiController]
[Route("identity-service")]
public class IdentityController : ControllerBase
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly RedisCacheService _redis;
    private readonly IMailService _mailService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConfiguration _configuration;
    
    public IdentityController(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        RedisCacheService redis,
        IMailService mailService,
        ITokenProvider tokenProvider,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
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
        
        await _redis.ConfirmationCodesDb.StringSetAsync(email, code);
        
        var mailData = _mailService.CreateBaseMail(email, code);
        
        if (await _mailService.SendMailAsync(mailData))
            return Ok(new {message = "Code was successfully sent"});
        
        return BadRequest(new {message = "Failed to send email code"});
    }

    [HttpPost("ConfirmCode")]
    public async Task<IActionResult> ConfirmCode(string email, int code)
    {
        if (await _redis.ConfirmationCodesDb.StringGetAsync(email) != code) 
            return BadRequest("Code was not confirmed");
        
        await _redis.EmailVerificationDb.StringSetAsync(email, true);
        await _redis.ConfirmationCodesDb.KeyDeleteAsync(email);
        return Ok("Code was confirmed");

    }

    [HttpPost("AuthenticateWithCode")]
    public async Task<IActionResult> AuthenticateWithCode(string email)
    {
        var user = await _userRepository.Find(u => u.EmailAddress == email);
        
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
        
        var role = await _roleRepository.Find(r => r.Name == "User");

        if (role == null)
            return BadRequest(new {message = "No role for user was found"});
        
        user = new User { EmailAddress = email, HashPassword = null, RoleId = role.Id};
        
        await _userRepository.Create(user);
        
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
        var user = await _userRepository.Find(u => u.EmailAddress == email);
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
        var user = await _userRepository.GetById(Guid.Parse(userId));
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
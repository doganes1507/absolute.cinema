using System.Security.Claims;
using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> SendEmailCode([FromBody] SendEmailCodeDto dto)
    {
        var rnd = new Random();
        var code = rnd.Next(100000, 999999);
        
        await _redis.ConfirmationCodesDb.StringSetAsync(dto.EmailAddress, code,
            TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:ConfirmationCodeExpirationInMinutes")));
        
        var mailData = _mailService.CreateBaseMail(dto.EmailAddress, code);
        
        if (await _mailService.SendMailAsync(mailData))
            return Ok(new {message = "Code was successfully sent"});
        
        return BadRequest(new {message = "Failed to send email code"});
    }

    [HttpPost("ConfirmCode")]
    public async Task<IActionResult> ConfirmCode([FromBody] ConfirmCodeDto dto)
    {
        if (await _redis.ConfirmationCodesDb.StringGetAsync(dto.EmailAddress) != dto.Code) 
            return BadRequest(new {message = "Code was not confirmed"});
        
        await _redis.EmailVerificationDb.StringSetAsync(dto.EmailAddress, true, TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:EmailVerificationExpirationInMinutes")));
        await _redis.ConfirmationCodesDb.KeyDeleteAsync(dto.EmailAddress);
        return Ok(new {message = "Code was confirmed"});
    }

    [HttpPost("AuthenticateWithCode")]
    public async Task<IActionResult> AuthenticateWithCode([FromBody] AuthenticateWithCodeDto dto)
    {
        var confirmed = await _redis.EmailVerificationDb.StringGetDeleteAsync(dto.EmailAddress);
        
        if (confirmed != true)
            return BadRequest(new {message = "Email wasn't verified"});

        var message = "User successfully logged in";
        
        var user = await _userRepository.FindAsync(u => u.EmailAddress == dto.EmailAddress);

        if (user == null)
        {
            var role = await _roleRepository.FindAsync(r => r.Name == "User");
            
            if (role == null)
                return BadRequest(new {message = "No role for user was found"});
            
            user = new User { EmailAddress = dto.EmailAddress, HashPassword = null, RoleId = role.Id };
            await _userRepository.CreateAsync(user);
        
            // Add user creation request to the message broker queue
            
            message = "User successfully registered";
        }
        
        var accessToken = _tokenProvider.GetAccessToken(user);
        var refreshToken = _tokenProvider.GetRefreshToken();
        
        await _redis.RefreshTokensDb.StringSetAsync(user.Id.ToString(), refreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")));
        
        return Ok(new 
        {
            accessToken,
            refreshToken,
            message
        });
    }

    [HttpPost("AuthenticateWithPassword")]
    public async Task<IActionResult> AuthenticateWithPassword([FromBody] AuthenticateWithPasswordDto dto)
    {        
        var user = await _userRepository.FindAsync(u => u.EmailAddress == dto.EmailAddress);

        if (user == null)
            return BadRequest(new { message = "User doesn’t exists" });
        
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.HashPassword))
            return Unauthorized(new { message = "Invalid credentials"});
        
        var accessToken = _tokenProvider.GetAccessToken(user);
        var refreshToken = _tokenProvider.GetRefreshToken();
        
        await _redis.RefreshTokensDb.StringSetAsync(user.Id.ToString(), refreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")));
        
        return Ok(new
        {
            accessToken,
            refreshToken,
            message = "User successfully logged in"
        });
    }

    [Authorize]
    [HttpPost("UpdateEmailAddress")]
    public async Task<IActionResult> UpdateEmailAddress([FromBody] UpdateEmailAddressDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (await _userRepository.AnyAsync(u => u.EmailAddress == dto.NewEmailAddress))
        {
            return BadRequest(new {message = "Email is already in use"});
        }
        
        if (_redis.EmailVerificationDb.StringGetAsync(dto.NewEmailAddress).Result != true)
        {
            return BadRequest(new {message = "New Email address was not verified"});
        }
        
        user.EmailAddress = dto.NewEmailAddress;
        await _userRepository.UpdateAsync(user);
        
        return Ok(new {message = "Email address updated"});
    }

    [Authorize]
    [HttpPost("UpdatePassword")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _userRepository.GetByIdAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found");

        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateAsync(user);
        
        return Ok("Password was successfully updated");
    }

    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var user = await _userRepository.GetByIdAsync(dto.UserId);

        if (user == null)
        {
            return NotFound("User not found");
        }

        if (await _redis.RefreshTokensDb.StringGetAsync(dto.UserId.ToString()) != dto.OldRefreshToken)
        {
            return BadRequest(new {message = "Refresh token is expired, invalid, or not found"});
        }

        var newAccessToken = _tokenProvider.GetAccessToken(user);
        var newRefreshToken = _tokenProvider.GetRefreshToken();

        await _redis.RefreshTokensDb.StringSetAsync(dto.UserId.ToString(), newRefreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")));

        return Ok(new
        {
            newAccessToken,
            newRefreshToken,
            message = ""
        });
    }
}
using System.Security.Claims;
using Absolute.Cinema.IdentityService.Data;
using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.DataObjects.IdentityController;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Absolute.Cinema.Shared.Interfaces;
using Absolute.Cinema.Shared.KafkaEvents;
using Absolute.Cinema.Shared.Models.Enumerations;
using KafkaFlow.Producers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.IdentityService.Controllers;

[ApiController]
[Route("identity-service")]
public class IdentityController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IMailService _mailService;
    private readonly ITokenProvider _tokenProvider;
    private readonly IConfiguration _configuration;
    private readonly IProducerAccessor _producerAccessor;
    private readonly ApplicationDbContext _dbContext;

    public IdentityController(
        ICacheService cacheService,
        IMailService mailService,
        ITokenProvider tokenProvider,
        IConfiguration configuration,
        IProducerAccessor producerAccessor,
        ApplicationDbContext dbContext)
    {
        _cacheService = cacheService;
        _mailService = mailService;
        _tokenProvider = tokenProvider;
        _configuration = configuration;
        _producerAccessor = producerAccessor;
        _dbContext = dbContext;
    }

    [HttpPost("SendEmailCode")]
    public async Task<IActionResult> SendEmailCode([FromBody] SendEmailCodeDto dto)
    {
        var rnd = new Random();
        var code = rnd.Next(100000, 999999);

        await _cacheService.SetAsync(
            dto.EmailAddress,
            code,
            TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:ConfirmationCodeExpirationInMinutes")),
            _configuration.GetValue<int>("Redis:ConfirmationCodesDatabaseId")
            );
        
        
        var mailData = _mailService.CreateBaseMail(dto.EmailAddress, code);
        
        if (await _mailService.SendMailAsync(mailData))
            return Ok(new {message = "Code was successfully sent"});
        
        return BadRequest(new {message = "Failed to send email code"});
    }

    [HttpPost("ConfirmCode")]
    public async Task<IActionResult> ConfirmCode([FromBody] ConfirmCodeDto dto)
    {
        var codeFromCache = await _cacheService.GetAsync<int>(
            dto.EmailAddress,
            _configuration.GetValue<int>("Redis:ConfirmationCodesDatabaseId")
            );
        
        if (codeFromCache != dto.Code)
            return BadRequest(new {message = "Invalid code"});

        await _cacheService.SetAsync(
            dto.EmailAddress,
            true,
            TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:EmailVerificationExpirationInMinutes")),
            _configuration.GetValue<int>("Redis:EmailVerificationDatabaseId")
        );

        await _cacheService.DeleteAsync(
            dto.EmailAddress,
            _configuration.GetValue<int>("Redis:ConfirmationCodesDatabaseId")
            );

        return Ok(new {message = "Code confirmed"});
    }

    [HttpPost("AuthenticateWithCode")]
    public async Task<IActionResult> AuthenticateWithCode([FromBody] AuthenticateWithCodeDto dto)
    {
        var confirmed = await _cacheService.GetDeleteAsync<bool>(
            dto.EmailAddress,
            _configuration.GetValue<int>("Redis:EmailVerificationDatabaseId"));
        if (confirmed != true)
            return BadRequest(new {message = "Email address is not verified"});

        var message = "User successfully logged in";
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmailAddress == dto.EmailAddress);

        if (user == null)
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "User");
            
            if (role == null)
                return BadRequest(new {message = "No role for user was found"});
            
            user = new User { EmailAddress = dto.EmailAddress, HashPassword = null, RoleId = role.Id};
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            
            var producer = _producerAccessor[_configuration["Kafka:ProducerName"]];
            await producer.ProduceAsync(
                messageKey: null,
                messageValue: new SyncUserEvent(user.Id, user.EmailAddress, DbOperation.Create));
            
            message = "User successfully registered";
        }
        
        var accessToken = _tokenProvider.GetAccessToken(user);
        var refreshToken = _tokenProvider.GetRefreshToken();

        await _cacheService.SetAsync(
            user.Id.ToString(),
            refreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")),
            _configuration.GetValue<int>("Redis:RefreshTokensDatabaseId")
            );
        
        return Ok(new 
        {
            accessToken,
            refreshToken,
            userId = user.Id.ToString(),
            message
        });
    }

    [HttpPost("AuthenticateWithPassword")]
    public async Task<IActionResult> AuthenticateWithPassword([FromBody] AuthenticateWithPasswordDto dto)
    {        
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.EmailAddress == dto.EmailAddress);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials" });
        
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.HashPassword))
            return Unauthorized(new { message = "Invalid credentials"});
        
        var accessToken = _tokenProvider.GetAccessToken(user);
        var refreshToken = _tokenProvider.GetRefreshToken();
        
        await _cacheService.SetAsync(
            user.Id.ToString(),
            refreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")),
            _configuration.GetValue<int>("Redis:RefreshTokensDatabaseId")
        );
        
        return Ok(new
        {
            accessToken,
            refreshToken,
            userId = user.Id.ToString(),
            message = "User successfully logged in"
        });
    }

    [Authorize]
    [HttpPost("UpdateEmailAddress")]
    public async Task<IActionResult> UpdateEmailAddress([FromBody] UpdateEmailAddressDto dto)
    {
        var getRequestsDbId = _configuration.GetValue<int>("Redis:GetRequestsDbId");
        var getRequestsTimeSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:GetRequestExpirationInMinutes"));
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound(new { message = "User not found" });

        if (await _dbContext.Users.AnyAsync(u => u.EmailAddress == dto.NewEmailAddress))
            return BadRequest(new {message = "New email address is already in use"});

        var verified = await _cacheService.GetAsync<bool>(
            dto.NewEmailAddress,
            _configuration.GetValue<int>("Redis:EmailVerificationDatabaseId")
        );
        if (verified != true)
            return BadRequest(new {message = "New email address is not verified"});
        
        user.EmailAddress = dto.NewEmailAddress;
        await _dbContext.SaveChangesAsync();

        var producer = _producerAccessor.GetProducer(_configuration.GetValue<string>("Kafka:ProducerName"));
        await producer.ProduceAsync(
            messageKey: null,
            messageValue: new SyncUserEvent(user.Id, user.EmailAddress, DbOperation.Update));
        
        if (_cacheService.IsConnected(getRequestsDbId))
        {
            await _cacheService.SetAsync<UserResponseDto?>(
                user.EmailAddress, 
                UserResponseDto.FormDto(user), 
                getRequestsTimeSpan, 
                getRequestsDbId);
            
            await _cacheService.SetAsync<UserResponseDto?>(
                user.Id.ToString(),
                UserResponseDto.FormDto(user),
                getRequestsTimeSpan, 
                getRequestsDbId);
        }
        
        var accessToken = _tokenProvider.GetAccessToken(user);
        var refreshToken = _tokenProvider.GetRefreshToken();
        
        await _cacheService.SetAsync(
            user.Id.ToString(),
            refreshToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")),
            _configuration.GetValue<int>("Redis:RefreshTokensDatabaseId")
        );
        
        return Ok(new
        {
            accessToken,
            refreshToken,
            message = "Email address updated"
        });
    }

    [Authorize]
    [HttpPost("UpdatePassword")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound(new {message = "User not found"});

        user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _dbContext.SaveChangesAsync();
        
        return Ok(new
        {
            userId = user.Id.ToString(),
            message = "Password successfully updated"
        });
    }

    [HttpPost("RefreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == dto.UserId);

        if (user == null)
            return NotFound("User not found");
        
        var cacheRefreshToken = await _cacheService.GetAsync<string>(
            dto.UserId.ToString(),
            _configuration.GetValue<int>("Redis:RefreshTokensDatabaseId")
            );
        if (cacheRefreshToken != dto.OldRefreshToken)
            return BadRequest(new {message = "Refresh token is expired, invalid, or not found"});
        

        var newAccessToken = _tokenProvider.GetAccessToken(user);
        var newRefreshToken = _tokenProvider.GetRefreshToken();

        await _cacheService.SetAsync(
            dto.UserId.ToString(),
            newAccessToken,
            TimeSpan.FromDays(_configuration.GetValue<int>("TokenSettings:RefreshToken:ExpirationInDays")),
            _configuration.GetValue<int>("Redis:RefreshTokensDatabaseId"));

        return Ok(new
        {
            newAccessToken,
            newRefreshToken,
            message = "Token successfully refreshed"
        });
    }
    
    [Authorize]
    [HttpDelete("DeleteUser")]
    public async Task<IActionResult> DeleteUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound(new {message = "User not found"});
        
        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
        
        var producer = _producerAccessor[_configuration["Kafka:ProducerName"]];
        await producer.ProduceAsync(
            messageKey: null,
            messageValue: new SyncUserEvent(user.Id, user.EmailAddress, DbOperation.Delete));

        await _cacheService.DeleteAsync(
            user.EmailAddress,
            _configuration.GetValue<int>("Redis:GetRequestsDbId")
        );

        await _cacheService.DeleteAsync(
            user.Id.ToString(),
            _configuration.GetValue<int>("Redis:GetRequestsDbId")
        );
        
        return Ok(new { message = "User successfully deleted" });
    }
    
}
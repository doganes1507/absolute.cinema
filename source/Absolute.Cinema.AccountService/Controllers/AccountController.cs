using System.Security.Claims;
using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.DataObjects;
using Absolute.Cinema.AccountService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absolute.Cinema.AccountService.Controllers;

[ApiController]
[Route("account-service")]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly RedisCacheService _cacheService;
    private readonly IConfiguration _configuration;
    
    public AccountController(ApplicationDbContext dbContext, RedisCacheService cacheService, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
        _configuration = configuration;
    }
    
    [Authorize]
    [HttpGet("GetPersonalInfo")]
    public async Task<IActionResult> GetPersonalInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var key = $"user:{userId}";
        var user = _cacheService.GetAsync<User>(key).Result;
        if (user != null)
            return Ok(user);
        
        // compare performance with: user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId));
        user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound();

        var expiry = TimeSpan.FromMinutes(_configuration.GetValue<int>("UserCacheTimeMinutes"));
        await _cacheService.SetAsync(key, user, expiry);
        
        return Ok(user);
    }
    
    [Authorize]
    [HttpPut("UpdatePersonalInfo")]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
        if (user == null)
            return NotFound("User not found.");
        
        if (dto.FirstName != null) user.FirstName = dto.FirstName;
        if (dto.BirthDate.HasValue) user.DateOfBirth = dto.BirthDate;
        if (dto.Gender.HasValue) user.Gender = dto.Gender.Value;
        
        await _dbContext.SaveChangesAsync();
        
        var key = $"user:{userId}";
        var expiry = TimeSpan.FromMinutes(_configuration.GetValue<int>("UserCacheTimeMinutes"));
        await _cacheService.SetAsync(key, user, expiry);
        
        return Ok(new {message = "Personal info updated successfully."});
    }
}
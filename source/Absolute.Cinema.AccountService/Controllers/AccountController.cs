using System.Security.Claims;
using Absolute.Cinema.AccountService.Data;
using Absolute.Cinema.AccountService.DataObjects;
using Absolute.Cinema.Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Absolute.Cinema.AccountService.Controllers;

[ApiController]
[Route("account-service")]
public class AccountController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICachedRepository _cachedRepository;

    public AccountController(ApplicationDbContext dbContext, ICachedRepository cachedRepository)
    {
        _dbContext = dbContext;
        _cachedRepository = cachedRepository;
    }
    
    [Authorize]
    [HttpGet("GetPersonalInfo")]
    public async Task<IActionResult> GetPersonalInfo()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _cachedRepository.ReadAsync(
            dbFetchFunc: async () => await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == Guid.Parse(userId)),
            cacheKey: userId
        );

        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }
    
    [Authorize]
    [HttpPut("UpdatePersonalInfo")]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();
        
        var user = await _cachedRepository.WriteAsync(
            dbWriteFunc: async () =>
            {
                var user = await _dbContext.Users.FindAsync(Guid.Parse(userId));
                if (user == null) return null;
                
                if (dto.FirstName != null) user.FirstName = dto.FirstName;
                if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth.Value;
                if (dto.Gender != null) user.Gender = dto.Gender.Value;
                
                await _dbContext.SaveChangesAsync();
                
                return user;
            },
            cacheKey: userId
        );
        
        if (user == null)
            return NotFound("User not found.");

        return Ok(new {message = "Personal info updated successfully."});
    }
}
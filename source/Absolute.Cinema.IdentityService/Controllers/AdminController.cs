using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Absolute.Cinema.Shared.Interfaces;
using Absolute.Cinema.Shared.KafkaEvents;
using KafkaFlow.Producers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absolute.Cinema.IdentityService.Controllers;

[ApiController]
[Route("identity-service/admin")]
public class AdminController : ControllerBase
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;
    private readonly IProducerAccessor _producerAccessor;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;

    public AdminController(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository,
        IProducerAccessor producerAccessor,
        IConfiguration configuration,
        ICacheService cacheService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _producerAccessor = producerAccessor;
        _configuration = configuration;
        _cacheService = cacheService;
    }

    [HttpPost("users")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == dto.Role);
        if (role == null)
            return BadRequest(new { message = "Such role does not exist." });
        
        if (await _userRepository.AnyAsync(u => u.EmailAddress == dto.EmailAddress))
            return BadRequest(new { message = "Such user already exist." });
        
        var user = new User
        {
            EmailAddress = dto.EmailAddress,
            RoleId = role.Id,
            HashPassword = dto.Password != null ? BCrypt.Net.BCrypt.HashPassword(dto.Password) : null
        };
        await _userRepository.CreateAsync(user);
        
        var producer = _producerAccessor.GetProducer(_configuration.GetValue<string>("KafkaSettings:ProducerName"));
        await producer.ProduceAsync(Guid.NewGuid().ToString(), new SyncUserEvent(user.Id, user.EmailAddress));
        
        return Ok(new { message = "User created successfully." });
    }
    
    [HttpPost("roles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (await _roleRepository.AnyAsync(r => r.Name == dto.RoleName))
            return BadRequest(new { message = "Role with this name already exist." });
        
        await _roleRepository.CreateAsync(new Role { Name = dto.RoleName });
        return Ok(new { message = "Role created successfully." });
    }
    
    [HttpGet("users")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUser([FromQuery]Guid? userId, [FromQuery] string? emailAddress)
    {
        var getRequestsDbId = _configuration.GetValue<int>("Redis:GetRequestsDbId");
        var getRequestsTimeSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:GetRequestExpirationInMinutes"));
        
        if (userId == null && emailAddress == null)
            return BadRequest(new { message = "Either userId or emailAddress must be provided." });
        
        if (_cacheService.IsConnected(getRequestsDbId))
        {
            var userCache = userId != null 
                ? await _cacheService.GetAsync<UserResponseDto?>(userId.ToString()!, getRequestsDbId)
                : await _cacheService.GetAsync<UserResponseDto?>(emailAddress!, getRequestsDbId);

            if (userCache != null)
                return Ok(userCache);
        }
        
        var user = userId != null 
            ? await _userRepository.GetByIdAsync(userId.Value)
            : await _userRepository.FindAsync(u => u.EmailAddress == emailAddress);

        if (user == null)
            return NotFound(new { message = "User not found." });

        var response = UserResponseDto.FormDto(user);
        if (_cacheService.IsConnected(getRequestsDbId))
        {
            await _cacheService.SetAsync<UserResponseDto?>(
                user.EmailAddress, 
                response, 
                getRequestsTimeSpan, 
                getRequestsDbId);
            
            await _cacheService.SetAsync<UserResponseDto?>(user.Id.ToString(),
                response, 
                getRequestsTimeSpan, 
                getRequestsDbId);
        }

        return Ok(response);
    }

    [HttpGet("roles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleRepository.GetAllAsync();
        return Ok(roles.Select(r => new RoleResponseDto(r)));
    }

    [HttpPut("users/{userId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UpdateUser([FromRoute] Guid userId, [FromBody] UpdateUserDto dto)
    {
        var getRequestsDbId = _configuration.GetValue<int>("Redis:GetRequestsDbId");
        var getRequestsTimeSpan = TimeSpan.FromMinutes(_configuration.GetValue<int>("Redis:GetRequestExpirationInMinutes"));
        
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (dto.NewRole != null)
        {
            var role = await _roleRepository.FindAsync(r => r.Name == dto.NewRole);
            
            if (role == null)
                return BadRequest(new { message = "Such role does not exist." });
            
            user.RoleId = role.Id;
        }

        if (dto.NewEmailAddress != null)
        {
            if (await _userRepository.AnyAsync(u => u.EmailAddress == dto.NewEmailAddress))
                return BadRequest(new { message = "This email already in use." });
            
            user.EmailAddress = dto.NewEmailAddress;
        }

        if (dto.NewPassword != null)
            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        await _userRepository.UpdateAsync(user);

        if (dto.NewEmailAddress == user.EmailAddress)
        {
            var producer = _producerAccessor.GetProducer(_configuration.GetValue<string>("KafkaSettings:ProducerName"));
            await producer.ProduceAsync(Guid.NewGuid().ToString(), new SyncUserEvent(user.Id, user.EmailAddress));
        }
        
        if (_cacheService.IsConnected(getRequestsDbId))
        {
            await _cacheService.SetAsync<UserResponseDto?>(
                user.EmailAddress, 
                UserResponseDto.FormDto(user), 
                getRequestsTimeSpan, 
                getRequestsDbId);
            
            await _cacheService.SetAsync<UserResponseDto?>(user.Id.ToString(),
                UserResponseDto.FormDto(user), 
                getRequestsTimeSpan, 
                getRequestsDbId);
        }
        

        return Ok(new { message = "User updated successfully." });
    }
    
    [HttpDelete("users/{userId}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        
        if (user == null)
            return NotFound(new { message = "User not found." });

        await _userRepository.RemoveAsync(user);
        
        return Ok(new { message = "User deleted successfully." });
    }

    [HttpDelete("roles/{roleName}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteRole([FromRoute] string roleName)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == roleName);
        
        if (role == null)
            return NotFound(new { message = "Role not found." });
        
        await _roleRepository.RemoveAsync(role);
        
        return Ok(new { message = "Role deleted successfully." });
    }
}
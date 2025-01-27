using Absolute.Cinema.IdentityService.DataObjects.AdminController;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absolute.Cinema.IdentityService.Controllers;

public class AdminController : ControllerBase
{
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Role> _roleRepository;

    public AdminController(
        IRepository<User> userRepository,
        IRepository<Role> roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    [HttpPost("CreateUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == dto.Role);
        if (role == null)
            return BadRequest(new { message = "Such role does not exist." });
        
        if (await _userRepository.AnyAsync(u => u.EmailAddress == dto.EmailAdress))
            return BadRequest(new { message = "Such user already exist." });
        
        await _userRepository.CreateAsync(new User
        {
            EmailAddress = dto.EmailAdress,  
            RoleId = role.Id,
            HashPassword = dto.Password != null ? BCrypt.Net.BCrypt.HashPassword(dto.Password) : null
        });
        
        return Ok(new { message = "User created successfully." });
    }
    
    [HttpPost("CreateRole")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto dto)
    {
        await _roleRepository.CreateAsync(new Role { Name = dto.RoleName });
        return Ok(new { message = "Role created successfully." });
    }
    
    [HttpGet("GetUserCredentials")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUserCredentials([FromQuery] GetOrDeleteUserDto dto)
    {
        var user = await GetUserByEmailOrId(dto);
        
        if (user == null)
            return NotFound(new { message = "User not found." });
        
        return Ok(new ResponseUserDto(user));
    }

    [HttpGet("GetAllRoles")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleRepository.GetAllAsync();
        return Ok(roles.Select(r => new ResponseRoleDto(r)));
    }

    [HttpPut("UpdateUserCredentials")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UpdateUserCredentials([FromBody] UpdateUserDto dto)
    {
        var user = await GetUserByEmailOrId(new GetOrDeleteUserDto {Email = dto.userEmail, UserId = dto.userId});
                
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
            if (await _userRepository.FindAsync(u => u.EmailAddress == dto.NewEmailAddress) == null)
                user.EmailAddress = dto.NewEmailAddress;
            else
                return BadRequest(new { message = "This email already in use." });
        }

        if (dto.NewPassword != null)
            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        
        await _userRepository.UpdateAsync(user);
        
        return Ok(new { message = "User updated successfully." });
            
    }
    
    [HttpDelete("DeleteUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteUser([FromQuery] GetOrDeleteUserDto dto)
    {
        var user = await GetUserByEmailOrId(dto);
        
        if (user == null)
            return NotFound(new { message = "User not found." });

        await _userRepository.RemoveAsync(user);
        
        return Ok(new { message = "User deleted successfully." });
    }

    [HttpDelete("DeleteRole")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteRole([FromQuery] DeleteRoleDto dto)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == dto.RoleName);
        
        if (role == null)
            return NotFound(new { message = "Role not found." });
        
        await _roleRepository.RemoveAsync(role);
        
        return Ok(new { message = "Role deleted successfully." });
    }
    
    private async Task<User?> GetUserByEmailOrId(GetOrDeleteUserDto dto)
    {
        if (dto.UserId != null)
            return await _userRepository.GetByIdAsync(Guid.Parse(dto.UserId));
        if (dto.Email != null)
            return await _userRepository.FindAsync(u => u.EmailAddress == dto.Email);

        return null;
    }


}
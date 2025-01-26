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
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto input)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == input.Role);
        if (role == null)
            return BadRequest(new { message = "Such role does not exist." });
        
        if (await _userRepository.FindAsync(u => u.EmailAddress == input.EmailAdress) != null)
            return BadRequest(new { message = "Such user already exist." });
        
        await _userRepository.CreateAsync(new User
        {
            EmailAddress = input.EmailAdress,  
            RoleId = role.Id,
            HashPassword = input.Password != null ? BCrypt.Net.BCrypt.HashPassword(input.Password) : null
        });
        
        return Ok(new { message = "User created successfully." });
    }
    
    [HttpPost("CreateRole")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleDto input)
    {
        await _roleRepository.CreateAsync(new Role { Name = input.RoleName });
        return Ok(new { message = "Role created successfully." });
    }
    
    [HttpGet("GetUserCredentials")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUserCredentials([FromQuery] GetOrDeleteUserDto input)
    {
        var user = await GetUserByEmailOrId(input);
        
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
    public async Task<IActionResult> UpdateUserCredentials([FromBody] UpdateUserDto input)
    {
        var user = await GetUserByEmailOrId(new GetOrDeleteUserDto {Email = input.userEmail, UserId = input.userId});
                
        if (user == null)
            return NotFound(new { message = "User not found." });

        if (input.NewRole != null)
        {
            var role = await _roleRepository.FindAsync(r => r.Name == input.NewRole);
            
            if (role == null)
                return BadRequest(new { message = "Such role does not exist." });
            
            user.RoleId = role.Id;
        }

        if (input.NewEmailAdress != null)
        {
            if (await _userRepository.FindAsync(u => u.EmailAddress == input.NewEmailAdress) == null)
                user.EmailAddress = input.NewEmailAdress;
            else
                return BadRequest(new { message = "This email already in use." });
        }

        if (input.NewPassword != null)
            user.HashPassword = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
        
        await _userRepository.UpdateAsync(user);
        
        return Ok(new { message = "User updated successfully." });
            
    }
    
    [HttpDelete("DeleteUser")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteUser([FromQuery] GetOrDeleteUserDto input)
    {
        var user = await GetUserByEmailOrId(input);
        
        if (user == null)
            return NotFound(new { message = "User not found." });

        await _userRepository.RemoveAsync(user);
        
        return Ok(new { message = "User deleted successfully." });
    }

    [HttpDelete("DeleteRole")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> DeleteRole([FromQuery] DeleteRoleDto input)
    {
        var role = await _roleRepository.FindAsync(r => r.Name == input.RoleName);
        
        if (role == null)
            return NotFound(new { message = "Role not found." });
        
        await _roleRepository.RemoveAsync(role);
        
        return Ok(new { message = "Role deleted successfully." });
    }
    
    private async Task<User?> GetUserByEmailOrId(GetOrDeleteUserDto input)
    {
        if (input.UserId != null)
            return await _userRepository.GetByIdAsync(Guid.Parse(input.UserId));
        if (input.Email != null)
            return await _userRepository.FindAsync(u => u.EmailAddress == input.Email);

        return null;
    }


}
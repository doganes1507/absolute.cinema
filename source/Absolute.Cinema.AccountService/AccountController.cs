using Absolute.Cinema.AccountService.DataObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Absolute.Cinema.AccountService;

[ApiController]
[Route("account-service")]
public class AccountController : ControllerBase
{
    [Authorize]
    [HttpGet("GetPersonalInfo")]
    public async Task<IActionResult> GetPersonalInfo()
    {
        throw new NotImplementedException();
    }
    
    [Authorize]
    [HttpPut("UpdatePersonalInfo")]
    public async Task<IActionResult> UpdatePersonalInfo([FromBody] UpdatePersonalInfoDto dto)
    {
        throw new NotImplementedException();
    }
}
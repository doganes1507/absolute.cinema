using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Absolute.Cinema.IdentityService.Services;

public class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    public string GetAccessToken(User user)
    {
        var secretKey = configuration["TokenSettings:AccessToken:SecretKey"];
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.EmailAddress),
                    new Claim(ClaimTypes.Role, user.Role.Name),
                ]
            ),
            SigningCredentials = credentials,
            Expires = DateTime.Now.AddMinutes(
                configuration.GetValue<int>("TokenSettings:AccessToken:ExpirationInMinutes")),
            Issuer = configuration["TokenSettings:Common:Issuer"]
        };
        
        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }

    public string GetRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }
}
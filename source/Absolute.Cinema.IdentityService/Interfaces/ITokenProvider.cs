using Absolute.Cinema.IdentityService.Models;

namespace Absolute.Cinema.IdentityService.Interfaces;

public interface ITokenProvider
{
    public string GetAccessToken(User user);
    public string GetRefreshToken();
    public string GetConfirmationToken(string emailAddress);
}
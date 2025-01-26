namespace Absolute.Cinema.IdentityService.DataObjects.IdentityController;

public class RefreshTokenDto
{
    public string UserId { get; set; }
    public string OldRefreshToken { get; set; }
}
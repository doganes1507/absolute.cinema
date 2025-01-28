namespace Absolute.Cinema.IdentityService.DataObjects.IdentityController;

public class RefreshTokenDto
{
    public Guid UserId { get; set; }
    public string OldRefreshToken { get; set; }
}
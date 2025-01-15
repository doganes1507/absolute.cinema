namespace Absolute.Cinema.IdentityService.Configuration;

public class MailConfiguration
{
    public string? DisplayName { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? MailFrom { get; set; }
    public string? SmtpServer { get; set; }
    public int SmtpPort { get; set; }
    public bool EnableSsl { get; set; }
    public bool EnableOAuth { get; set; }
}
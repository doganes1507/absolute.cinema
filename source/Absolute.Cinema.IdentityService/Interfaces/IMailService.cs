using Absolute.Cinema.IdentityService.Models;

namespace Absolute.Cinema.IdentityService.Interfaces;

public interface IMailService
{
    Task<bool> SendMailAsync(MailData mailData);

    public MailData CreateBaseMail(string email, int code);
}
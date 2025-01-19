using Absolute.Cinema.IdentityService.Configuration;
using Absolute.Cinema.IdentityService.Interfaces;
using Absolute.Cinema.IdentityService.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Absolute.Cinema.IdentityService.Services;

public class MailService(IOptions<MailConfiguration> mailConfig) : IMailService
{
    private readonly MailConfiguration _mailConfig = mailConfig.Value;
    
    public async Task<bool> SendMailAsync(MailData mailData)
    {

        var message = new MimeMessage();

        try {
            message.From.Add(new MailboxAddress(_mailConfig.DisplayName ?? _mailConfig.UserName, _mailConfig.MailFrom));
            message.Sender = new MailboxAddress(_mailConfig.DisplayName ?? _mailConfig.UserName, _mailConfig.MailFrom);
            message.To.Add(MailboxAddress.Parse(mailData.To));
        }
        catch (ParseException) {
            // Добавить при реализации логирования Unable to parse mail address
            return false;
        }

        var body = new BodyBuilder();
        message.Subject = mailData.Subject;
        body.HtmlBody = mailData.Body;
        message.Body = body.ToMessageBody();

        using var client = new SmtpClient();
            
        try {
            await client.ConnectAsync(_mailConfig.SmtpServer, _mailConfig.SmtpPort, SecureSocketOptions.SslOnConnect);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            await client.AuthenticateAsync(_mailConfig.UserName, _mailConfig.Password);
            await client.SendAsync(message);
        }
        catch (Exception e) {
            // Добавить при реализации логирования Unable to send mail
            Console.WriteLine(e);
            return false;
        }
        finally {
            await client.DisconnectAsync(true);
        }

        return true;

    }
    
    public MailData CreateBaseMail(string email, int code)
    {
        var subject = "Confirmation code";
        var body = $@"
        <!doctype html>
        <html lang='en'>
          <head>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <meta http-equiv='Content-Type' content='text/html; charset=UTF-8'>
            <title>Simple Transactional Email</title>
            <style>
              body {{ font-family: Helvetica, sans-serif; font-size: 16px; background-color: #f4f5f6; }}
              .header {{ max-width: 600px; margin: 12px; padding: 24px; background: #ffa000; border-radius: 16px; }}
              .container {{ max-width: 600px; margin: 12px; padding: 24px; background: #bbbbbb; border-radius: 16px; }}
              .btn-primary a {{ background-color: #0867ec; color: #ffffff; padding: 12px 24px; text-decoration: none; }}
            </style>
          </head>
          <body>
            <div class='header'>
              <h1>Verify your email address</h1>
            </div>
            <div class='container'>
              <p>Enter the following code to verify your email address:</p>
              <h2 style='color: #0867ec;'>{code}</h2>
              <p>If you did not request this, please ignore this email.</p>
              <p>Thank you!</p>
            </div>
          </body>
        </html>";
        
        var mailDate = new MailData(email, subject, body);
        
        return mailDate;
    }
}
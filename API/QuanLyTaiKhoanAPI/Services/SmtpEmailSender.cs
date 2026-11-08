using QuanLyTaiKhoanAPI.Interfaces;
using System.Net;
using System.Net.Mail;

namespace QuanLyTaiKhoanAPI.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"];
        var fromEmail = _configuration["Smtp:FromEmail"];
        var fromName = _configuration["Smtp:FromName"] ?? "Quan Ly Tai Khoan";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException("SMTP is not configured.");
        }

        var port = int.TryParse(_configuration["Smtp:Port"], out var configuredPort)
            ? configuredPort
            : 587;
        var enableSsl = bool.TryParse(_configuration["Smtp:EnableSsl"], out var configuredSsl)
            ? configuredSsl
            : true;
        var userName = _configuration["Smtp:UserName"];
        var password = _configuration["Smtp:Password"];

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };

        if (!string.IsNullOrWhiteSpace(userName))
        {
            client.Credentials = new NetworkCredential(userName, password);
        }

        await client.SendMailAsync(message, cancellationToken);
    }
}

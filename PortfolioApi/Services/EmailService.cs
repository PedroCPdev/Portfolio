using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Portfolio.Models;

namespace Portfolio.Services;

public class EmailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
}

public class EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
{
    private readonly EmailOptions _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.Host) &&
        !string.IsNullOrWhiteSpace(_options.Username) &&
        !string.IsNullOrWhiteSpace(_options.ToAddress);

    public async Task SendContactMessageAsync(ContactMessage message)
    {
        if (!IsConfigured)
        {
            logger.LogWarning(
                "Email section not configured; contact message from {Email} was logged but not emailed",
                message.Email);
            return;
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl = true,
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.Username, "Portfolio Contact Form"),
            Subject = $"New contact message from {message.Name}",
            Body = $"From: {message.Name} <{message.Email}>\n\n{message.Message}",
        };
        mail.To.Add(_options.ToAddress);
        mail.ReplyToList.Add(new MailAddress(message.Email, message.Name));

        await client.SendMailAsync(mail);
    }
}

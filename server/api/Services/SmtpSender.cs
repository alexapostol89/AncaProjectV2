using api.Configuration;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace api.Services;

/// <summary>Sends one ready-made email over SMTP. An interface so tests can record emails instead of sending them.</summary>
public interface ISmtpSender
{
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// SMTP with STARTTLS (port 587) using MailKit. Used for Gmail:
///   Email__SmtpHost=smtp.gmail.com, Email__SmtpUsername=&lt;the Gmail address&gt;,
///   Email__SmtpPassword=&lt;a Google "app password", not the normal Gmail password&gt;.
/// </summary>
public class MailKitSmtpSender : ISmtpSender
{
    private readonly EmailSettings _settings;

    public MailKitSmtpSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(MimeMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient { Timeout = 15_000 };
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, cancellationToken);
        await client.SendAsync(new SendRequest(message), cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}

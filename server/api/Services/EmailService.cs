using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using api.Configuration;
using api.DTOs;
using Microsoft.Extensions.Options;
using MimeKit;

namespace api.Services;

/// <summary>
/// Builds the site's emails and sends them through SMTP (Gmail) when Email:SmtpHost is set,
/// otherwise through Resend's REST API (https://resend.com/docs/api-reference/emails/send-email).
/// Only this class knows about the providers; the rest of the app just calls IEmailService.
/// </summary>
public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly ISmtpSender _smtp;

    public EmailService(
        HttpClient httpClient,
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger,
        ISmtpSender smtp)
    {
        _httpClient = httpClient;
        _smtp = smtp;
        _settings = settings.Value;
        _logger = logger;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri("https://api.resend.com/");
        }

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }
    }

    public async Task SendContactNotificationAsync(ContactRequest request, CancellationToken cancellationToken = default)
    {
        var submittedAt = DateTime.UtcNow;

        await SendAsync(BuildNotificationEmail(request, submittedAt), cancellationToken);

        // Confirmation email is a nice-to-have for the visitor; if it fails
        // we don't want to fail the whole request (the notification already
        // succeeded, so the psychologist still received the message).
        try
        {
            await SendAsync(BuildConfirmationEmail(request), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Confirmation email failed to send, notification email already succeeded.");
        }
    }

    public async Task SendBookingRequestedAsync(string fullName, string email, DateTimeOffset start, string reviewLink, CancellationToken cancellationToken = default)
    {
        var name = HtmlEncoder.Default.Encode(fullName);
        var safeEmail = HtmlEncoder.Default.Encode(email);
        var href = HtmlEncoder.Default.Encode(reviewLink);
        var when = FormatRomaniaTime(start);

        // 1) The practice: a request is waiting for approval.
        var practiceRecipients = _settings.BookingRequestTo.Where(a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (practiceRecipients.Count == 0) practiceRecipients = [_settings.To];

        await SendAsync(new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = practiceRecipients,
            ReplyTo = email,
            Subject = $"Ai o cerere nouă de programare — {fullName}",
            Html = $"""
                <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
                  <h2>Ai o cerere nouă de programare</h2>
                  <p>Un client a solicitat o ședință și așteaptă confirmarea ta.</p>
                  <p><strong>Nume:</strong> {name}</p>
                  <p><strong>Email:</strong> {safeEmail}</p>
                  <p><strong>Data:</strong> {when} (ora României)</p>
                  <p>Ședința <strong>nu</strong> este încă în calendar. Intervalul este rezervat până când o aprobi sau o respingi.</p>
                  <p style="margin:24px 0;">
                    <a href="{href}" style="background:#7a8b6f;color:#ffffff;padding:12px 20px;border-radius:8px;text-decoration:none;display:inline-block;">Vezi cererile de programare</a>
                  </p>
                </div>
                """
        }, cancellationToken);

        // 2) The client: we got it, wait for confirmation. Nice-to-have, so failures are only logged.
        try
        {
            await SendAsync(new ResendEmailPayload
            {
                From = $"{_settings.FromName} <{_settings.From}>",
                To = [email],
                Subject = "Am primit cererea ta de programare — Cabinet Psihologie",
                Html = $"""
                    <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
                      <p>Bună, {name},</p>
                      <p>Am primit cererea ta pentru o ședință pe <strong>{when}</strong> (ora României).</p>
                      <p>Cererea așteaptă confirmarea mea. Vei primi un email și o invitație în calendar imediat ce ședința este confirmată.</p>
                      <p>Cu drag,<br/>Anca Alexandra Berbunschi<br/>Psiholog · Psihoterapeut Integrativ</p>
                    </div>
                    """
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Booking request acknowledgement to the client failed; the practice was notified.");
        }
    }

    public Task SendBookingApprovedAsync(string fullName, string email, DateTimeOffset start, string? videoConferenceUrl, CancellationToken cancellationToken = default)
    {
        var name = HtmlEncoder.Default.Encode(fullName);
        var when = FormatRomaniaTime(start);
        var videoLink = string.IsNullOrWhiteSpace(videoConferenceUrl) ? "" : $"<p>Google Meet: <a href=\"{HtmlEncoder.Default.Encode(videoConferenceUrl)}\">Join the video session</a></p>";
        return SendAsync(new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = [email],
            Subject = "Ședința ta este confirmată — Cabinet Psihologie",
            Html = $"""
                {videoLink}
                <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
                  <p>Bună, {name},</p>
                  <p>Ședința ta din <strong>{when}</strong> (ora României) este confirmată.</p>
                  <p>Ai primit și o invitație Google Calendar separată cu toate detaliile.</p>
                  <p>Cu drag,<br/>Anca Alexandra Berbunschi<br/>Psiholog · Psihoterapeut Integrativ</p>
                </div>
                """
        }, cancellationToken);
    }

    public Task SendBookingRejectedAsync(string fullName, string email, DateTimeOffset start, string? reason, CancellationToken cancellationToken = default)
    {
        var name = HtmlEncoder.Default.Encode(fullName);
        var when = FormatRomaniaTime(start);
        var reasonHtml = string.IsNullOrWhiteSpace(reason)
            ? string.Empty
            : $"""<p style="background:#f7f5f0;padding:12px 16px;border-radius:8px;white-space:pre-line;">{HtmlEncoder.Default.Encode(reason)}</p>""";
        return SendAsync(new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = [email],
            Subject = "Cererea ta de programare — Cabinet Psihologie",
            Html = $"""
                <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
                  <p>Bună, {name},</p>
                  <p>Din păcate nu pot confirma ședința din <strong>{when}</strong> (ora României).</p>
                  {reasonHtml}
                  <p>Te rog să alegi un alt interval pe site sau să-mi scrii direct.</p>
                  <p>Cu drag,<br/>Anca Alexandra Berbunschi<br/>Psiholog · Psihoterapeut Integrativ</p>
                </div>
                """
        }, cancellationToken);
    }

    private static string FormatRomaniaTime(DateTimeOffset start)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");
        return TimeZoneInfo.ConvertTime(start, zone).ToString("dd.MM.yyyy HH:mm");
    }

    private async Task SendAsync(ResendEmailPayload payload, CancellationToken cancellationToken)
    {
        if (_settings.UseSmtp)
        {
            await SendWithSmtpAsync(payload, cancellationToken);
            return;
        }

        var response = await _httpClient.PostAsJsonAsync("emails", payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            // Intentionally not logging message contents (see privacy note below),
            // only the failure metadata needed to diagnose delivery issues.
            _logger.LogError(
                "Resend API call failed with status {StatusCode}. Response: {Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException($"Email provider returned {(int)response.StatusCode}.");
        }
    }

    private async Task SendWithSmtpAsync(ResendEmailPayload payload, CancellationToken cancellationToken)
    {
        var message = new MimeMessage { Subject = payload.Subject };
        message.From.Add(MailboxAddress.Parse(payload.From));
        message.To.AddRange(payload.To.Select(MailboxAddress.Parse));
        if (!string.IsNullOrWhiteSpace(payload.ReplyTo))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(payload.ReplyTo));
        }
        message.Body = new BodyBuilder { HtmlBody = payload.Html }.ToMessageBody();

        try
        {
            await _smtp.SendAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Same contract as the Resend path: log the reason, throw so callers decide what to do.
            _logger.LogError(ex, "SMTP send via {Host} failed.", _settings.SmtpHost);
            throw new InvalidOperationException("Email provider failed.", ex);
        }
    }

    private ResendEmailPayload BuildNotificationEmail(ContactRequest request, DateTime submittedAt)
    {
        var name = HtmlEncoder.Default.Encode(request.Name);
        var email = HtmlEncoder.Default.Encode(request.Email);
        var phone = HtmlEncoder.Default.Encode(request.Phone ?? "—");
        var sessionType = HtmlEncoder.Default.Encode(request.SessionType);
        var message = HtmlEncoder.Default.Encode(request.Message).Replace("\n", "<br/>");

        var html = $"""
            <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
              <h2 style="margin-bottom:4px;">Solicitare nouă de contact</h2>
              <p style="color:#777;margin-top:0;">Cabinet Psihologie — Anca Alexandra Berbunschi</p>
              <table style="border-collapse:collapse;margin-top:16px;">
                <tr><td style="padding:4px 12px 4px 0;"><strong>Nume:</strong></td><td>{name}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;"><strong>Email:</strong></td><td>{email}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;"><strong>Telefon:</strong></td><td>{phone}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;"><strong>Tip ședință:</strong></td><td>{sessionType}</td></tr>
                <tr><td style="padding:4px 12px 4px 0;"><strong>Data:</strong></td><td>{submittedAt:dd.MM.yyyy HH:mm} UTC</td></tr>
              </table>
              <p style="margin-top:16px;"><strong>Mesaj:</strong></p>
              <p style="background:#f7f5f0;padding:12px 16px;border-radius:8px;">{message}</p>
            </div>
            """;

        return new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = [_settings.To],
            ReplyTo = request.Email,
            Subject = "Solicitare nouă de contact — Cabinet Psihologie",
            Html = html
        };
    }

    private ResendEmailPayload BuildConfirmationEmail(ContactRequest request)
    {
        var name = HtmlEncoder.Default.Encode(request.Name);

        // Deliberately does NOT echo back the visitor's message — no need to
        // put potentially sensitive personal/psychological content into a
        // second email thread.
        var html = $"""
            <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
              <p>Bună, {name},</p>
              <p>Mulțumesc pentru mesaj. Am primit solicitarea ta și voi reveni cât mai curând, de obicei în cel mult 1-2 zile lucrătoare.</p>
              <p>Dacă te confrunți cu o urgență, te rog să nu aștepți acest răspuns și să contactezi direct serviciile de urgență (112) sau un specialist disponibil imediat.</p>
              <p>Cu drag,<br/>Anca Alexandra Berbunschi<br/>Psiholog · Psihoterapeut Integrativ</p>
            </div>
            """;

        return new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = [request.Email],
            Subject = "Am primit mesajul tău — Cabinet Psihologie",
            Html = html
        };
    }

    private class ResendEmailPayload
    {
        [JsonPropertyName("from")]
        public string From { get; set; } = string.Empty;

        [JsonPropertyName("to")]
        public List<string> To { get; set; } = [];

        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("html")]
        public string Html { get; set; } = string.Empty;
    }
}

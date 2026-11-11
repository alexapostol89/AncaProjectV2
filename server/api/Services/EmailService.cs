using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using api.Configuration;
using api.DTOs;
using Microsoft.Extensions.Options;

namespace api.Services;

/// <summary>
/// Sends emails through Resend's REST API (https://resend.com/docs/api-reference/emails/send-email).
/// Only this class knows about Resend; swapping providers later means
/// touching this file only, not the controller.
/// </summary>
public class EmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IHostEnvironment _environment;

    public EmailService(
        HttpClient httpClient,
        IOptions<EmailSettings> settings,
        ILogger<EmailService> logger,
        IHostEnvironment environment)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _environment = environment;

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

    public Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink, CancellationToken cancellationToken = default)
    {
        // In Development without an API key, print the link instead of emailing it,
        // so "forgot password" can be tested locally. Never happens in production.
        if (_environment.IsDevelopment() && string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _logger.LogWarning("DEV: Email:ApiKey not set, reset email not sent. Link: {Link}", resetLink);
            return Task.CompletedTask;
        }

        var name = HtmlEncoder.Default.Encode(fullName);
        var href = HtmlEncoder.Default.Encode(resetLink);

        var html = $"""
            <div style="font-family:sans-serif;line-height:1.6;color:#2b2b2b;">
              <p>Bună, {name},</p>
              <p>Am primit o solicitare de resetare a parolei pentru contul tău. Apasă butonul de mai jos pentru a alege o parolă nouă. Linkul este valabil o oră.</p>
              <p style="margin:24px 0;">
                <a href="{href}" style="background:#7a8b6f;color:#ffffff;padding:12px 20px;border-radius:8px;text-decoration:none;display:inline-block;">Resetează parola</a>
              </p>
              <p style="color:#777;font-size:14px;">Dacă butonul nu funcționează, copiază acest link în browser:<br/><span style="word-break:break-all;">{href}</span></p>
              <p style="color:#777;font-size:14px;">Dacă nu tu ai solicitat resetarea, poți ignora acest mesaj — parola ta rămâne neschimbată.</p>
              <p>Cu drag,<br/>Anca Alexandra Berbunschi<br/>Psiholog · Psihoterapeut Integrativ</p>
            </div>
            """;

        return SendAsync(new ResendEmailPayload
        {
            From = $"{_settings.FromName} <{_settings.From}>",
            To = [toEmail],
            Subject = "Resetare parolă — Cabinet Psihologie",
            Html = html
        }, cancellationToken);
    }

    private async Task SendAsync(ResendEmailPayload payload, CancellationToken cancellationToken)
    {
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

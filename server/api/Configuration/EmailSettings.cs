namespace api.Configuration;

/// <summary>
/// Strongly-typed binding for the "Email" configuration section.
/// Populated from appsettings.json (dev) or environment variables (prod):
///   Email__ApiKey, Email__From, Email__To, Email__FromName, Email__Smtp*
/// Two ways to send, picked automatically:
///   - SMTP (e.g. Gmail) when <see cref="SmtpHost"/> is set — can send from a Gmail address to anyone
///   - otherwise Resend (<see cref="ApiKey"/>) — needs a domain verified in Resend to email anyone but the account owner
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>Resend API key (server-side only, never exposed to the client).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Sender address. With Gmail SMTP it must be the Gmail account itself; with Resend, an address on a verified domain.</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Display name used alongside the "From" address.</summary>
    public string FromName { get; set; } = "Cabinet Psihologie Anca Berbunschi";

    /// <summary>Psychologist's inbox that should receive contact notifications.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Who is told about new session requests (Email:BookingRequestTo:0, :1, ...).
    /// Falls back to <see cref="To"/> when empty.
    /// </summary>
    public List<string> BookingRequestTo { get; set; } = [];

    /// <summary>SMTP server, e.g. smtp.gmail.com. When set, emails go through SMTP instead of Resend.</summary>
    public string SmtpHost { get; set; } = string.Empty;

    /// <summary>587 = STARTTLS (what Gmail expects).</summary>
    public int SmtpPort { get; set; } = 587;

    public string SmtpUsername { get; set; } = string.Empty;

    /// <summary>For Gmail: an "app password" (Google account → Security → App passwords), never the real password.</summary>
    public string SmtpPassword { get; set; } = string.Empty;

    public bool UseSmtp => !string.IsNullOrWhiteSpace(SmtpHost);

    /// <summary>False when neither SMTP nor Resend is set up (local development).</summary>
    public bool CanSend => UseSmtp || !string.IsNullOrWhiteSpace(ApiKey);
}

namespace api.Configuration;

/// <summary>
/// Strongly-typed binding for the "Email" configuration section.
/// Populated from appsettings.json (dev) or environment variables (prod):
///   Email__ApiKey, Email__From, Email__To, Email__FromName
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    /// <summary>Resend API key (server-side only, never exposed to the client).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender address in Resend, e.g. contact@yourdomain.ro</summary>
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
}

namespace api.Configuration;

/// <summary>
/// Strongly-typed binding for the "Jwt" configuration section.
/// Key is a secret: user-secrets in development, Fly secret (Jwt__Key) in production.
/// Never put it in appsettings.json.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "anca-api";

    public string Audience { get; set; } = "anca-client";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>How long a login lasts. After this the user has to log in again.</summary>
    public int ExpiryHours { get; set; } = 8;
}

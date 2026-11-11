namespace api.Configuration;

/// <summary>
/// Where the React frontend lives. Used to build the link in password-reset
/// emails: {BaseUrl}/reset-password?email=...&amp;token=...
/// </summary>
public class ClientAppSettings
{
    public const string SectionName = "ClientApp";

    public string BaseUrl { get; set; } = "http://localhost:5173";
}

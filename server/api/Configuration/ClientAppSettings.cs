namespace api.Configuration;

/// <summary>
/// Where the React frontend lives. Used to build links in emails,
/// e.g. {BaseUrl}/admin/programari in the "new booking request" email.
/// </summary>
public class ClientAppSettings
{
    public const string SectionName = "ClientApp";

    public string BaseUrl { get; set; } = "http://localhost:5173";
}

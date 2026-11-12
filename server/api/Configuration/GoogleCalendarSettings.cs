namespace api.Configuration;

/// <summary>Server-side OAuth credentials for the practice Google Calendar.</summary>
public sealed class GoogleCalendarSettings
{
    public const string SectionName = "GoogleCalendar";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string CalendarId { get; set; } = "primary";
    public string TimeZone { get; set; } = "Europe/Bucharest";
    public string PsychologistEmail { get; set; } = "psihologancaalexandra@gmail.com";
    public int SessionMinutes { get; set; } = 50;
}

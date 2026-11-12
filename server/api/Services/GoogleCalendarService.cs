using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using api.Configuration;
using Microsoft.Extensions.Options;

namespace api.Services;

public sealed record CalendarSlot(DateTimeOffset Start, DateTimeOffset End);

public interface IGoogleCalendarService
{
    Task<IReadOnlyList<CalendarSlot>> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken);
    /// <summary>Creates the calendar event (inviting the client) and returns its Google event id.</summary>
    Task<string> CreateBookingAsync(string name, string email, DateTimeOffset start, CancellationToken cancellationToken);
}

/// <summary>Calls Google's OAuth and Calendar REST APIs using a server-only refresh token.</summary>
public sealed class GoogleCalendarService(HttpClient http, IOptions<GoogleCalendarSettings> options, ILogger<GoogleCalendarService> logger) : IGoogleCalendarService
{
    private readonly GoogleCalendarSettings _settings = options.Value;
    private static readonly SemaphoreSlim BookingGate = new(1, 1);
    private static readonly TimeZoneInfo RomaniaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");

    public async Task<IReadOnlyList<CalendarSlot>> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken)
    {
        if (date.DayOfWeek == DayOfWeek.Sunday) return [];
        var token = await AccessTokenAsync(cancellationToken);
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), RomaniaTimeZone.GetUtcOffset(date.ToDateTime(TimeOnly.MinValue)));
        var dayEnd = dayStart.AddDays(1);
        var busy = await GetBusyAsync(token, dayStart, dayEnd, cancellationToken);
        var slots = new List<CalendarSlot>();
        var localStart = date.ToDateTime(new TimeOnly(10, 0));
        var localEnd = date.ToDateTime(new TimeOnly(20, 0));
        var duration = TimeSpan.FromMinutes(_settings.SessionMinutes);
        for (var local = localStart; local.Add(duration) <= localEnd; local = local.Add(duration))
        {
            var start = new DateTimeOffset(local, RomaniaTimeZone.GetUtcOffset(local));
            var end = start.Add(duration);
            if (start <= DateTimeOffset.UtcNow || busy.Any(x => start < x.End && end > x.Start)) continue;
            slots.Add(new CalendarSlot(start, end));
        }
        return slots;
    }

    public async Task<string> CreateBookingAsync(string name, string email, DateTimeOffset start, CancellationToken cancellationToken)
    {
        await BookingGate.WaitAsync(cancellationToken);
        try
        {
            var token = await AccessTokenAsync(cancellationToken);
            var end = start.AddMinutes(_settings.SessionMinutes);
            var busy = await GetBusyAsync(token, start.AddMinutes(-1), end.AddMinutes(1), cancellationToken);
            if (busy.Any(x => start < x.End && end > x.Start)) throw new CalendarSlotUnavailableException();

            using var request = new HttpRequestMessage(HttpMethod.Post, $"https://www.googleapis.com/calendar/v3/calendars/{Uri.EscapeDataString(_settings.CalendarId)}/events?sendUpdates=all")
            {
                Content = JsonContent.Create(new
                {
                    summary = "Ședință de psihologie",
                    description = "Ședință programată prin ancaberbunschi.ro.",
                    start = new { dateTime = start.ToString("o"), timeZone = _settings.TimeZone },
                    end = new { dateTime = end.ToString("o"), timeZone = _settings.TimeZone },
                    attendees = new[] { new { email, displayName = (string?)name } }
                })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Google Calendar event creation failed with status {StatusCode}.", (int)response.StatusCode);
                throw new InvalidOperationException("Calendar event could not be created.");
            }
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            return json.RootElement.GetProperty("id").GetString() ?? string.Empty;
        }
        finally
        {
            BookingGate.Release();
        }
    }

    private async Task<List<CalendarSlot>> GetBusyAsync(string token, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://www.googleapis.com/calendar/v3/freeBusy")
        {
            Content = JsonContent.Create(new
            {
                timeMin = from.ToString("o"), timeMax = to.ToString("o"), timeZone = _settings.TimeZone,
                items = new[] { new { id = _settings.CalendarId } }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Calendar availability could not be loaded.");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var calendar = json.RootElement.GetProperty("calendars").GetProperty(_settings.CalendarId);
        var output = new List<CalendarSlot>();
        foreach (var item in calendar.GetProperty("busy").EnumerateArray())
            output.Add(new CalendarSlot(item.GetProperty("start").GetDateTimeOffset(), item.GetProperty("end").GetDateTimeOffset()));
        return output;
    }

    private async Task<string> AccessTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId) || string.IsNullOrWhiteSpace(_settings.ClientSecret) || string.IsNullOrWhiteSpace(_settings.RefreshToken))
            throw new CalendarNotConfiguredException();
        using var response = await http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId, ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = _settings.RefreshToken, ["grant_type"] = "refresh_token"
        }), cancellationToken);
        if (!response.IsSuccessStatusCode) throw new CalendarNotConfiguredException();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return json.RootElement.GetProperty("access_token").GetString()!;
    }
}

public sealed class CalendarSlotUnavailableException : Exception { }
public sealed class CalendarNotConfiguredException : Exception { }

using System.Net;
using System.Text.Json;
using api.Configuration;
using api.Services;
using api.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace api.Tests.Services;

/// <summary>The real Google Calendar client, checked against a fake Google (OAuth token, freeBusy, events).</summary>
public sealed class GoogleCalendarServiceTests
{
    private readonly FakeHttpHandler _google = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private GoogleCalendarService CreateService(string clientId = "client-id", string secret = "secret", string refreshToken = "refresh") => new(
        new HttpClient(_google),
        Options.Create(new GoogleCalendarSettings { ClientId = clientId, ClientSecret = secret, RefreshToken = refreshToken, CalendarId = "primary" }),
        NullLogger<GoogleCalendarService>.Instance);

    private FakeHttpHandler GoogleWorks(string busyJson = "[]", string eventId = "evt-123") => _google
        .On("oauth2.googleapis.com/token", HttpStatusCode.OK, """{"access_token":"access-1","expires_in":3599}""")
        .On("freeBusy", HttpStatusCode.OK, """{"calendars":{"primary":{"busy":""" + busyJson + "}}}")
        .On("/events", HttpStatusCode.OK, $$"""{"id":"{{eventId}}"}""");

    private static string Busy(DateTimeOffset start, int minutes = 50) =>
        $$"""[{"start":"{{start.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}}","end":"{{start.AddMinutes(minutes).UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}}"}]""";

    [Theory]
    [InlineData("", "secret", "refresh")]
    [InlineData("client-id", "", "refresh")]
    [InlineData("client-id", "secret", "")]
    public async Task Missing_credentials_mean_not_configured_and_google_is_never_called(string id, string secret, string refresh)
    {
        await Assert.ThrowsAsync<CalendarNotConfiguredException>(
            () => CreateService(id, secret, refresh).GetAvailabilityAsync(_day, CancellationToken.None));

        Assert.Empty(_google.Requests);
    }

    [Fact]
    public async Task Refresh_token_rejected_by_google_means_not_configured()
    {
        _google.On("oauth2.googleapis.com/token", HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        await Assert.ThrowsAsync<CalendarNotConfiguredException>(
            () => CreateService().GetAvailabilityAsync(_day, CancellationToken.None));
    }

    [Fact]
    public async Task Token_request_sends_the_refresh_token_grant()
    {
        GoogleWorks();

        await CreateService().GetAvailabilityAsync(_day, CancellationToken.None);

        var token = Assert.Single(_google.To("oauth2.googleapis.com/token"));
        Assert.Contains("grant_type=refresh_token", token.Body);
        Assert.Contains("refresh_token=refresh", token.Body);
        Assert.Contains("client_id=client-id", token.Body);
    }

    [Fact]
    public async Task Free_day_returns_twelve_slots_and_uses_the_access_token()
    {
        GoogleWorks();

        var slots = await CreateService().GetAvailabilityAsync(_day, CancellationToken.None);

        Assert.Equal(12, slots.Count);
        Assert.Equal(TestTime.At(_day, 10, 0), slots[0].Start);
        Assert.Equal("Bearer access-1", Assert.Single(_google.To("freeBusy")).Authorization);
    }

    [Fact]
    public async Task Busy_time_in_google_removes_overlapping_slots()
    {
        // 11:00–12:00 overlaps the 10:50 and 11:40 slots.
        GoogleWorks(Busy(TestTime.At(_day, 11, 0), 60));

        var slots = await CreateService().GetAvailabilityAsync(_day, CancellationToken.None);

        Assert.Equal(10, slots.Count);
        Assert.DoesNotContain(slots, s => s.Start == TestTime.At(_day, 10, 50));
        Assert.DoesNotContain(slots, s => s.Start == TestTime.At(_day, 11, 40));
        Assert.Contains(slots, s => s.Start == TestTime.At(_day, 10, 0));
    }

    [Fact]
    public async Task Sunday_returns_nothing_without_calling_google()
    {
        var slots = await CreateService().GetAvailabilityAsync(TestTime.FutureSunday(), CancellationToken.None);

        Assert.Empty(slots);
        Assert.Empty(_google.Requests);
    }

    [Fact]
    public async Task FreeBusy_failure_throws()
    {
        _google.On("oauth2.googleapis.com/token", HttpStatusCode.OK, """{"access_token":"a"}""")
               .On("freeBusy", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().GetAvailabilityAsync(_day, CancellationToken.None));
    }

    [Fact]
    public async Task Create_booking_returns_the_google_event_id()
    {
        GoogleWorks(eventId: "abc123");

        var id = await CreateService().CreateBookingAsync("Ioana", "ioana@example.com", TestTime.Slot(_day, 2), CancellationToken.None);

        Assert.Equal("abc123", id);
    }

    [Fact]
    public async Task Create_booking_invites_the_client_and_asks_google_to_send_the_invitation()
    {
        GoogleWorks();
        var start = TestTime.Slot(_day, 2);

        await CreateService().CreateBookingAsync("Ioana", "ioana@example.com", start, CancellationToken.None);

        var request = Assert.Single(_google.To("/events"));
        Assert.Contains("sendUpdates=all", request.Uri.Query);
        using var body = JsonDocument.Parse(request.Body);
        var attendee = body.RootElement.GetProperty("attendees")[0];
        Assert.Equal("ioana@example.com", attendee.GetProperty("email").GetString());
        Assert.Equal("Ioana", attendee.GetProperty("displayName").GetString());
        Assert.Equal(start, body.RootElement.GetProperty("start").GetProperty("dateTime").GetDateTimeOffset());
        Assert.Equal(start.AddMinutes(50), body.RootElement.GetProperty("end").GetProperty("dateTime").GetDateTimeOffset());
    }

    [Fact]
    public async Task Create_booking_refuses_a_slot_that_is_busy_in_google()
    {
        var start = TestTime.Slot(_day, 2);
        GoogleWorks(Busy(start));

        await Assert.ThrowsAsync<CalendarSlotUnavailableException>(
            () => CreateService().CreateBookingAsync("Ioana", "ioana@example.com", start, CancellationToken.None));

        Assert.Empty(_google.To("/events"));
    }

    [Fact]
    public async Task Create_booking_throws_when_google_rejects_the_event()
    {
        _google.On("oauth2.googleapis.com/token", HttpStatusCode.OK, """{"access_token":"a"}""")
               .On("freeBusy", HttpStatusCode.OK, """{"calendars":{"primary":{"busy":[]}}}""")
               .On("/events", HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService().CreateBookingAsync("Ioana", "ioana@example.com", TestTime.Slot(_day, 2), CancellationToken.None));
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.DTOs.Bookings;
using api.Models;
using api.Tests.Infrastructure;

namespace api.Tests.Api;

/// <summary>Client endpoints over HTTP: /api/bookings/availability, POST /api/bookings, /api/bookings/mine.</summary>
public sealed class BookingsEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    // ---- Login required ---------------------------------------------------

    [Theory]
    [InlineData("GET", "/api/bookings/availability?date=2030-01-07")]
    [InlineData("GET", "/api/bookings/mine")]
    [InlineData("POST", "/api/bookings")]
    public async Task Booking_endpoints_require_login(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST") request.Content = JsonContent.Create(new { start = TestTime.Slot(_day, 0) });

        var response = await _api.Anonymous().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Forged_token_is_rejected()
    {
        var client = _api.Anonymous();
        client.DefaultRequestHeaders.Authorization = new("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJ4In0.invalid");

        var response = await client.GetAsync("/api/bookings/mine");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Availability ----------------------------------------------------------

    [Fact]
    public async Task Availability_returns_the_free_slots()
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.GetAsync($"/api/bookings/availability?date={_day:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ApiFactory.ReadAsync<AvailabilityResponse>(response);
        Assert.Equal(12, body!.Slots.Count);
        Assert.Equal(TestTime.Slot(_day, 0), body.Slots[0].Start);
    }

    [Fact]
    public async Task Availability_hides_slots_that_another_client_requested()
    {
        var (other, _) = await _api.NewClientAsync("Other");
        await _api.AddBookingAsync(other.Id, TestTime.Slot(_day, 0));
        var (_, client) = await _api.NewClientAsync();

        var body = await ApiFactory.ReadAsync<AvailabilityResponse>(await client.GetAsync($"/api/bookings/availability?date={_day:yyyy-MM-dd}"));

        Assert.Equal(11, body!.Slots.Count);
        Assert.DoesNotContain(body.Slots, s => s.Start == TestTime.Slot(_day, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(91)]
    public async Task Availability_outside_the_next_90_days_is_a_bad_request(int daysFromToday)
    {
        var (_, client) = await _api.NewClientAsync();
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysFromToday));

        var response = await client.GetAsync($"/api/bookings/availability?date={date:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Availability_when_google_is_not_connected_returns_503_with_code()
    {
        _api.Calendar.NotConfigured = true;
        var (_, client) = await _api.NewClientAsync();

        var response = await client.GetAsync($"/api/bookings/availability?date={_day:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("calendar_not_configured", await ErrorCode(response));
    }

    [Fact]
    public async Task Availability_when_google_errors_returns_502()
    {
        _api.Calendar.AvailabilityThrows = new HttpRequestException("boom");
        var (_, client) = await _api.NewClientAsync();

        var response = await client.GetAsync($"/api/bookings/availability?date={_day:yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("calendar_unavailable", await ErrorCode(response));
    }

    // ---- Sending a request -------------------------------------------------------

    [Fact]
    public async Task Posting_a_free_slot_creates_a_pending_request()
    {
        var (user, client) = await _api.NewClientAsync();
        var start = TestTime.Slot(_day, 2);

        var response = await client.PostAsJsonAsync("/api/bookings", new { start });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await ApiFactory.ReadAsync<MyBookingDto>(response);
        Assert.Equal("Pending", booking!.Status);
        Assert.Equal(start, booking.Start);
        var saved = await _api.FindBookingAsync(booking.Id);
        Assert.Equal(user.Id, saved!.UserId);
        Assert.Empty(_api.Calendar.Created);
        Assert.Single(_api.Email.OfKind("Requested"));
    }

    [Fact]
    public async Task Posting_a_taken_slot_returns_409_slot_taken()
    {
        var (other, _) = await _api.NewClientAsync("Other");
        await _api.AddBookingAsync(other.Id, TestTime.Slot(_day, 2));
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 2) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("slot_taken", await ErrorCode(response));
    }

    [Fact]
    public async Task Fourth_pending_request_returns_409_too_many_pending()
    {
        var (_, client) = await _api.NewClientAsync();
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, i) })).StatusCode);

        var response = await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 3) });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("too_many_pending", await ErrorCode(response));
    }

    public static TheoryData<string, DateTimeOffset> InvalidTimes()
    {
        var day = TestTime.FutureWorkday();
        return new TheoryData<string, DateTimeOffset>
        {
            { "Sunday", TestTime.At(TestTime.FutureSunday(), 10, 0) },
            { "before 10:00", TestTime.At(day, 9, 10) },
            { "ends after 20:00", TestTime.At(day, 19, 30) },
            { "not on the hourly grid", TestTime.At(day, 10, 30) },
            { "with seconds", TestTime.At(day, 10, 0).AddSeconds(15) },
            { "in the past", DateTimeOffset.UtcNow.AddDays(-1) },
            { "more than 90 days ahead", TestTime.At(TestTime.FutureWorkday(100), 10, 0) },
        };
    }

    [Theory]
    [MemberData(nameof(InvalidTimes))]
    public async Task Invalid_times_return_400_invalid_time(string why, DateTimeOffset start)
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/bookings", new { start });

        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, $"Expected 400 for a time {why}, got {(int)response.StatusCode}");
        Assert.Equal("invalid_time", await ErrorCode(response));
    }

    [Fact]
    public async Task Missing_start_is_a_validation_error()
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/bookings", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Posting_when_google_is_not_connected_returns_503()
    {
        _api.Calendar.NotConfigured = true;
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 0) });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("calendar_not_configured", await ErrorCode(response));
    }

    [Fact]
    public async Task Token_of_a_deleted_user_cannot_book()
    {
        var (user, client) = await _api.NewClientAsync();
        await using (var db = _api.CreateDbContext())
        {
            db.Users.Remove(db.Users.Single(u => u.Id == user.Id));
            await db.SaveChangesAsync();
        }

        var response = await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 0) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- My requests ------------------------------------------------------------

    [Fact]
    public async Task Mine_lists_only_the_logged_in_clients_requests()
    {
        var (me, client) = await _api.NewClientAsync("Me");
        var (other, _) = await _api.NewClientAsync("Other");
        await _api.AddBookingAsync(me.Id, TestTime.Slot(_day, 1));
        await _api.AddBookingAsync(me.Id, TestTime.Slot(_day, 4), BookingStatus.Approved);
        await _api.AddBookingAsync(other.Id, TestTime.Slot(_day, 2));

        var mine = await ApiFactory.ReadAsync<List<MyBookingDto>>(await client.GetAsync("/api/bookings/mine"));

        Assert.Equal(2, mine!.Count);
        Assert.Equal(["Pending", "Approved"], mine.Select(b => b.Status));
    }

    [Fact]
    public async Task Booking_endpoints_are_rate_limited_per_ip()
    {
        var (_, client) = await _api.NewClientAsync();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 11; i++) statuses.Add((await client.GetAsync("/api/bookings/mine")).StatusCode);

        Assert.All(statuses.Take(10), s => Assert.Equal(HttpStatusCode.OK, s));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[10]);
    }

    private sealed record AvailabilityResponse(List<SlotDto> Slots);
    private sealed record SlotDto(DateTimeOffset Start, DateTimeOffset End);

    public void Dispose() => _api.Dispose();
}

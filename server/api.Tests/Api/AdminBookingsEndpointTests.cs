using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using api.DTOs.Bookings;
using api.Models;
using api.Tests.Infrastructure;

namespace api.Tests.Api;

/// <summary>Admin endpoints over HTTP: list, approve and reject. Only admins may use them.</summary>
public sealed class AdminBookingsEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
    }

    private async Task<Booking> PendingAsync(int slot = 3)
    {
        var client = await _api.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        return await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, slot));
    }

    // ---- Access control ------------------------------------------------------------

    public static TheoryData<string, string> AdminEndpoints() => new()
    {
        { "GET", "/api/admin/bookings" },
        { "POST", $"/api/admin/bookings/{Guid.NewGuid()}/approve" },
        { "POST", $"/api/admin/bookings/{Guid.NewGuid()}/reject" },
    };

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Anonymous_visitors_get_401(string method, string url)
    {
        var response = await _api.Anonymous().SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdminEndpoints))]
    public async Task Normal_clients_get_403(string method, string url)
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { }) });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_approve_their_own_request()
    {
        var (user, client) = await _api.NewClientAsync();
        var booking = await _api.AddBookingAsync(user.Id, TestTime.Slot(_day, 0));

        var response = await client.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(BookingStatus.Pending, (await _api.FindBookingAsync(booking.Id))!.Status);
        Assert.Empty(_api.Calendar.Created);
    }

    // ---- Listing -----------------------------------------------------------------------

    [Fact]
    public async Task Admin_lists_pending_requests_with_client_details()
    {
        await PendingAsync(slot: 5);
        var (_, admin) = await _api.NewAdminAsync();

        var list = await ApiFactory.ReadAsync<List<AdminBookingDto>>(await admin.GetAsync("/api/admin/bookings?status=Pending"));

        var item = Assert.Single(list!);
        Assert.Equal("Pending", item.Status);
        Assert.Equal("Ioana Ionescu", item.ClientName);
        Assert.Equal("ioana@example.com", item.ClientEmail);
        Assert.Equal(TestTime.Slot(_day, 5), item.Start);
    }

    [Fact]
    public async Task Admin_list_without_status_returns_all()
    {
        var client = await _api.AddUserAsync();
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 0), BookingStatus.Pending);
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 1), BookingStatus.Approved);
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 2), BookingStatus.Rejected);
        var (_, admin) = await _api.NewAdminAsync();

        var list = await ApiFactory.ReadAsync<List<AdminBookingDto>>(await admin.GetAsync("/api/admin/bookings"));

        Assert.Equal(3, list!.Count);
    }

    [Fact]
    public async Task Unknown_status_filter_is_a_bad_request()
    {
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.GetAsync("/api/admin/bookings?status=Maybe");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Approve --------------------------------------------------------------------

    [Fact]
    public async Task Approve_returns_the_approved_booking_and_creates_the_event()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Approved", (await ApiFactory.ReadAsync<AdminBookingDto>(response))!.Status);
        Assert.Equal("ioana@example.com", Assert.Single(_api.Calendar.Created).Email);
        Assert.Single(_api.Email.OfKind("Approved"));
    }

    [Fact]
    public async Task Approving_twice_returns_409_not_pending()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();
        await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("booking_not_pending", await ErrorCode(response));
        Assert.Single(_api.Calendar.Created);
    }

    [Fact]
    public async Task Approve_unknown_booking_returns_404()
    {
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{Guid.NewGuid()}/approve", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Approve_when_the_slot_is_busy_in_google_returns_409_calendar_busy()
    {
        var booking = await PendingAsync();
        _api.Calendar.MarkBusy(TestTime.Slot(_day, 3));
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("calendar_busy", await ErrorCode(response));
        Assert.Equal(BookingStatus.Pending, (await _api.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Approve_when_google_is_not_connected_returns_503()
    {
        var booking = await PendingAsync();
        _api.Calendar.NotConfigured = true;
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("calendar_not_configured", await ErrorCode(response));
    }

    [Fact]
    public async Task Approve_when_google_fails_returns_502_and_keeps_the_request_pending()
    {
        var booking = await PendingAsync();
        _api.Calendar.CreateThrows = new HttpRequestException("Google is down");
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(BookingStatus.Pending, (await _api.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Approve_past_request_returns_409_in_past()
    {
        var client = await _api.AddUserAsync();
        var booking = await _api.AddBookingAsync(client.Id, DateTimeOffset.UtcNow.AddHours(-3));
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("booking_in_past", await ErrorCode(response));
    }

    // ---- Reject -----------------------------------------------------------------------

    [Fact]
    public async Task Reject_with_reason_frees_the_slot_and_emails_the_client()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/reject", new { reason = "Concediu" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = await ApiFactory.ReadAsync<AdminBookingDto>(response);
        Assert.Equal("Rejected", dto!.Status);
        Assert.Equal("Concediu", dto.RejectionReason);
        Assert.Equal("Concediu", Assert.Single(_api.Email.OfKind("Rejected")).Extra);
        Assert.Empty(_api.Calendar.Created);
    }

    [Fact]
    public async Task Reject_without_reason_is_allowed()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/reject", new { reason = (string?)null });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Reject_reason_longer_than_500_characters_is_a_validation_error()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/reject", new { reason = new string('x', 501) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(BookingStatus.Pending, (await _api.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Reject_approved_booking_returns_409()
    {
        var booking = await PendingAsync();
        var (_, admin) = await _api.NewAdminAsync();
        await admin.PostAsync($"/api/admin/bookings/{booking.Id}/approve", null);

        var response = await admin.PostAsJsonAsync($"/api/admin/bookings/{booking.Id}/reject", new { reason = "x" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("booking_not_pending", await ErrorCode(response));
    }

    [Fact]
    public async Task Reject_unknown_booking_returns_404()
    {
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PostAsJsonAsync($"/api/admin/bookings/{Guid.NewGuid()}/reject", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _api.Dispose();
}

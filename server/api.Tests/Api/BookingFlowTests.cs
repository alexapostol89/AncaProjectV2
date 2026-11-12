using System.Net;
using System.Net.Http.Json;
using api.DTOs.Bookings;
using api.Tests.Infrastructure;

namespace api.Tests.Api;

/// <summary>
/// Whole journeys through the HTTP API, step by step, as a client and the admin would actually do them:
/// look at times → request → admin sees it → approve/reject → client sees the result.
/// </summary>
public sealed class BookingFlowTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private async Task<List<DateTimeOffset>> FreeSlotsAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<AvailabilityResponse>($"/api/bookings/availability?date={_day:yyyy-MM-dd}", ApiFactory.Json);
        return response!.Slots.Select(s => s.Start).ToList();
    }

    [Fact]
    public async Task Request_then_approve_books_the_session_and_sends_every_notification()
    {
        var (_, client) = await _api.NewClientAsync("Ioana Ionescu");
        var (_, admin) = await _api.NewAdminAsync();

        // 1. Client sees a free day and picks the 14:10 slot.
        var slots = await FreeSlotsAsync(client);
        var chosen = slots.Single(s => s == TestTime.At(_day, 14, 10));

        // 2. Client sends the request → Pending, practice notified, nothing in Google yet.
        var requested = await ApiFactory.ReadAsync<MyBookingDto>(await client.PostAsJsonAsync("/api/bookings", new { start = chosen }));
        Assert.Equal("Pending", requested!.Status);
        Assert.Single(_api.Email.OfKind("Requested"));
        Assert.Empty(_api.Calendar.Created);

        // 3. The slot is held: nobody else can see or request it.
        var (_, otherClient) = await _api.NewClientAsync("Someone Else");
        Assert.DoesNotContain(chosen, await FreeSlotsAsync(otherClient));
        Assert.Equal(HttpStatusCode.Conflict, (await otherClient.PostAsJsonAsync("/api/bookings", new { start = chosen })).StatusCode);

        // 4. Client sees their request as awaiting approval.
        var mine = await client.GetFromJsonAsync<List<MyBookingDto>>("/api/bookings/mine", ApiFactory.Json);
        Assert.Equal("Pending", Assert.Single(mine!).Status);

        // 5. Admin sees it in the pending list.
        var pending = await admin.GetFromJsonAsync<List<AdminBookingDto>>("/api/admin/bookings?status=Pending", ApiFactory.Json);
        var toReview = Assert.Single(pending!);
        Assert.Equal(requested.Id, toReview.Id);
        Assert.Equal("Ioana Ionescu", toReview.ClientName);

        // 6. Admin approves → Google event + confirmation email.
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/admin/bookings/{toReview.Id}/approve", null)).StatusCode);
        Assert.Equal(chosen, Assert.Single(_api.Calendar.Created).Start);
        Assert.Single(_api.Email.OfKind("Approved"));

        // 7. Client now sees it confirmed; the pending list is empty; the slot stays taken.
        mine = await client.GetFromJsonAsync<List<MyBookingDto>>("/api/bookings/mine", ApiFactory.Json);
        Assert.Equal("Approved", Assert.Single(mine!).Status);
        Assert.Empty((await admin.GetFromJsonAsync<List<AdminBookingDto>>("/api/admin/bookings?status=Pending", ApiFactory.Json))!);
        Assert.DoesNotContain(chosen, await FreeSlotsAsync(otherClient));
    }

    [Fact]
    public async Task Request_then_reject_frees_the_slot_and_another_client_can_take_it()
    {
        var (_, client) = await _api.NewClientAsync("Ioana Ionescu");
        var (_, admin) = await _api.NewAdminAsync();
        var chosen = TestTime.Slot(_day, 0);

        // 1. Client requests 10:00.
        var requested = await ApiFactory.ReadAsync<MyBookingDto>(await client.PostAsJsonAsync("/api/bookings", new { start = chosen }));

        // 2. Admin rejects with a reason → client emailed, no calendar event.
        var reject = await admin.PostAsJsonAsync($"/api/admin/bookings/{requested!.Id}/reject", new { reason = "Am un curs în acea dimineață." });
        Assert.Equal(HttpStatusCode.OK, reject.StatusCode);
        Assert.Equal("Am un curs în acea dimineață.", Assert.Single(_api.Email.OfKind("Rejected")).Extra);
        Assert.Empty(_api.Calendar.Created);

        // 3. Client sees "declined" with the reason.
        var mine = await client.GetFromJsonAsync<List<MyBookingDto>>("/api/bookings/mine", ApiFactory.Json);
        var declined = Assert.Single(mine!);
        Assert.Equal("Rejected", declined.Status);
        Assert.Equal("Am un curs în acea dimineață.", declined.RejectionReason);

        // 4. The slot is free again and someone else can request it.
        var (_, otherClient) = await _api.NewClientAsync("Someone Else");
        Assert.Contains(chosen, await FreeSlotsAsync(otherClient));
        Assert.Equal(HttpStatusCode.OK, (await otherClient.PostAsJsonAsync("/api/bookings", new { start = chosen })).StatusCode);
    }

    [Fact]
    public async Task Admin_handles_several_requests_independently()
    {
        var (_, alice) = await _api.NewClientAsync("Alice");
        var (_, bob) = await _api.NewClientAsync("Bob");
        var (_, admin) = await _api.NewAdminAsync();

        var a = await ApiFactory.ReadAsync<MyBookingDto>(await alice.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 1) }));
        var b = await ApiFactory.ReadAsync<MyBookingDto>(await bob.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 2) }));

        await admin.PostAsync($"/api/admin/bookings/{a!.Id}/approve", null);
        await admin.PostAsJsonAsync($"/api/admin/bookings/{b!.Id}/reject", new { reason = (string?)null });

        Assert.Equal("Approved", Assert.Single((await alice.GetFromJsonAsync<List<MyBookingDto>>("/api/bookings/mine", ApiFactory.Json))!).Status);
        Assert.Equal("Rejected", Assert.Single((await bob.GetFromJsonAsync<List<MyBookingDto>>("/api/bookings/mine", ApiFactory.Json))!).Status);
        Assert.Single(_api.Calendar.Created);
        Assert.Equal(2, _api.Email.OfKind("Requested").Count);
        Assert.Single(_api.Email.OfKind("Approved"));
        Assert.Single(_api.Email.OfKind("Rejected"));
    }

    [Fact]
    public async Task Calendar_conflict_at_approval_lets_the_admin_reject_instead()
    {
        var (_, client) = await _api.NewClientAsync();
        var (_, admin) = await _api.NewAdminAsync();
        var chosen = TestTime.Slot(_day, 6);
        var requested = await ApiFactory.ReadAsync<MyBookingDto>(await client.PostAsJsonAsync("/api/bookings", new { start = chosen }));

        // The psychologist blocks that time directly in Google Calendar before reviewing.
        _api.Calendar.MarkBusy(chosen);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/admin/bookings/{requested!.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/admin/bookings/{requested.Id}/reject", new { reason = "Interval ocupat." })).StatusCode);

        Assert.Empty(_api.Calendar.Created);
        Assert.Empty(_api.Email.OfKind("Approved"));
        Assert.Single(_api.Email.OfKind("Rejected"));
    }

    private sealed record AvailabilityResponse(List<SlotDto> Slots);
    private sealed record SlotDto(DateTimeOffset Start, DateTimeOffset End);

    public void Dispose() => _api.Dispose();
}

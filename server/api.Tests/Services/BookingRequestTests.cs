using api.Models;
using api.Services;
using api.Services.Results;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>Step 2 — the client picks a slot and sends a request. It must stay Pending: no calendar event yet.</summary>
public sealed class BookingRequestTests : BookingServiceTestBase
{
    [Fact]
    public async Task Request_is_saved_as_pending_with_correct_utc_times()
    {
        var user = await Db.AddUserAsync();
        var start = TestTime.Slot(Day, 1);

        var result = await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Pending", result.Value!.Status);
        Assert.Equal(start, result.Value.Start);
        Assert.Equal(start.AddMinutes(50), result.Value.End);

        var saved = await Db.FindBookingAsync(result.Value.Id);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.Pending, saved.Status);
        Assert.Equal(user.Id, saved.UserId);
        Assert.Equal(start.UtcDateTime, saved.StartUtc);
        Assert.Null(saved.DecidedAt);
        Assert.Null(saved.GoogleEventId);
    }

    [Fact]
    public async Task Request_does_not_create_a_google_calendar_event()
    {
        var user = await Db.AddUserAsync();

        await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 1), CancellationToken.None);

        Assert.Empty(Calendar.Created);
    }

    [Fact]
    public async Task Request_notifies_the_practice_with_a_link_to_the_admin_page()
    {
        var user = await Db.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        var start = TestTime.Slot(Day, 6);

        await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        var email = Assert.Single(Email.OfKind("Requested"));
        Assert.Equal("Ioana Ionescu", email.Name);
        Assert.Equal("ioana@example.com", email.Email);
        Assert.Equal(start, email.Start);
        Assert.Equal($"{ClientBaseUrl}/admin/programari", email.Extra);
        Assert.Empty(Email.OfKind("Approved"));
    }

    [Fact]
    public async Task Request_still_succeeds_when_the_notification_email_fails()
    {
        var user = await Db.AddUserAsync();
        Email.ThrowOnRequested = true;

        var result = await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 1), CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotNull(await Db.FindBookingAsync(result.Value!.Id));
    }

    [Fact]
    public async Task Slot_already_pending_for_someone_else_is_refused()
    {
        var first = await Db.AddUserAsync("First");
        var second = await Db.AddUserAsync("Second");
        var start = TestTime.Slot(Day, 2);
        await CreateService().RequestAsync(first.Id, start, CancellationToken.None);

        var result = await CreateService().RequestAsync(second.Id, start, CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("slot_taken", result.Error!.Code);
        Assert.Equal(ServiceErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Same_client_cannot_request_the_same_slot_twice()
    {
        var user = await Db.AddUserAsync();
        var start = TestTime.Slot(Day, 2);
        await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        var again = await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        Assert.Equal("slot_taken", again.Error!.Code);
    }

    [Fact]
    public async Task Slot_busy_in_google_calendar_is_refused()
    {
        var user = await Db.AddUserAsync();
        var start = TestTime.Slot(Day, 7);
        Calendar.MarkBusy(start);

        var result = await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        Assert.Equal("slot_taken", result.Error!.Code);
    }

    [Fact]
    public async Task Slot_that_was_rejected_before_can_be_requested_again()
    {
        var earlier = await Db.AddUserAsync("Earlier");
        var user = await Db.AddUserAsync();
        var start = TestTime.Slot(Day, 3);
        await Db.AddBookingAsync(earlier.Id, start, BookingStatus.Rejected);

        var result = await CreateService().RequestAsync(user.Id, start, CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Time_that_is_not_on_the_slot_grid_is_refused()
    {
        var user = await Db.AddUserAsync();

        var result = await CreateService().RequestAsync(user.Id, TestTime.At(Day, 10, 15), CancellationToken.None);

        Assert.Equal("slot_taken", result.Error!.Code);
    }

    [Fact]
    public async Task Unknown_user_is_refused()
    {
        var result = await CreateService().RequestAsync(Guid.NewGuid(), TestTime.Slot(Day, 1), CancellationToken.None);

        Assert.Equal("user_not_found", result.Error!.Code);
        Assert.Equal(ServiceErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Client_can_have_at_most_three_pending_requests()
    {
        var user = await Db.AddUserAsync();
        for (var i = 0; i < 3; i++)
            Assert.True((await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, i), CancellationToken.None)).Succeeded);

        var fourth = await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 3), CancellationToken.None);

        Assert.Equal("too_many_pending", fourth.Error!.Code);
    }

    [Fact]
    public async Task Approved_and_rejected_bookings_do_not_count_towards_the_pending_limit()
    {
        var user = await Db.AddUserAsync();
        await Db.AddBookingAsync(user.Id, TestTime.Slot(Day, 0), BookingStatus.Approved);
        await Db.AddBookingAsync(user.Id, TestTime.Slot(Day, 1), BookingStatus.Rejected);
        await Db.AddBookingAsync(user.Id, TestTime.Slot(Day, 2));
        await Db.AddBookingAsync(user.Id, TestTime.Slot(Day, 3));

        var result = await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 4), CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Old_pending_requests_in_the_past_do_not_count_towards_the_limit()
    {
        var user = await Db.AddUserAsync();
        var past = TestTime.At(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), 10, 0);
        for (var i = 0; i < 3; i++) await Db.AddBookingAsync(user.Id, past.AddMinutes(i * 50));

        var result = await CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 0), CancellationToken.None);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Calendar_not_configured_is_passed_to_the_controller()
    {
        var user = await Db.AddUserAsync();
        Calendar.NotConfigured = true;

        await Assert.ThrowsAsync<CalendarNotConfiguredException>(
            () => CreateService().RequestAsync(user.Id, TestTime.Slot(Day, 1), CancellationToken.None));
    }
}

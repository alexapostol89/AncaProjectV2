using api.Models;
using api.Services;
using api.Services.Results;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>
/// Step 4 — the admin approves a request. Only now is the Google Calendar event created
/// (which invites the client) and the confirmation email sent.
/// </summary>
public sealed class BookingApprovalTests : BookingServiceTestBase
{
    private async Task<(User Client, User Admin, Booking Booking)> PendingRequestAsync(int slot = 3)
    {
        var client = await Db.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        var admin = await Db.AddUserAsync("Anca Admin", role: UserRole.Admin);
        var booking = await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, slot));
        return (client, admin, booking);
    }

    [Fact]
    public async Task Approve_marks_the_booking_approved_and_records_who_and_when()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        var before = DateTime.UtcNow.AddSeconds(-1);

        var result = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Approved", result.Value!.Status);
        var saved = await Db.FindBookingAsync(booking.Id);
        Assert.Equal(BookingStatus.Approved, saved!.Status);
        Assert.Equal(admin.Id, saved.DecidedByUserId);
        Assert.True(saved.DecidedAt >= before);
    }

    [Fact]
    public async Task Approve_creates_the_google_event_for_the_client_and_stores_its_id()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        var created = Assert.Single(Calendar.Created);
        Assert.Equal("Ioana Ionescu", created.Name);
        Assert.Equal("ioana@example.com", created.Email);
        Assert.Equal(TestTime.Slot(Day, 3), created.Start);
        Assert.Equal(created.EventId, (await Db.FindBookingAsync(booking.Id))!.GoogleEventId);
    }

    [Fact]
    public async Task Approve_emails_the_client_a_confirmation()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        var email = Assert.Single(Email.OfKind("Approved"));
        Assert.Equal("ioana@example.com", email.Email);
        Assert.Equal(TestTime.Slot(Day, 3), email.Start);
        Assert.Empty(Email.OfKind("Rejected"));
    }

    [Fact]
    public async Task Approval_still_succeeds_when_the_confirmation_email_fails()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Email.ThrowOnApproved = true;

        var result = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(BookingStatus.Approved, (await Db.FindBookingAsync(booking.Id))!.Status);
        Assert.Single(Calendar.Created);
    }

    [Fact]
    public async Task Approving_twice_creates_only_one_calendar_event()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        var second = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.Equal("booking_not_pending", second.Error!.Code);
        Assert.Single(Calendar.Created);
        Assert.Single(Email.OfKind("Approved"));
    }

    [Fact]
    public async Task Rejected_request_cannot_be_approved()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        var result = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.Equal("booking_not_pending", result.Error!.Code);
        Assert.Empty(Calendar.Created);
    }

    [Fact]
    public async Task Unknown_booking_returns_not_found()
    {
        var admin = await Db.AddUserAsync(role: UserRole.Admin);

        var result = await CreateService().ApproveAsync(admin.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.Equal("booking_not_found", result.Error!.Code);
        Assert.Equal(ServiceErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Request_whose_time_has_passed_cannot_be_approved()
    {
        var client = await Db.AddUserAsync();
        var admin = await Db.AddUserAsync(role: UserRole.Admin);
        var booking = await Db.AddBookingAsync(client.Id, DateTimeOffset.UtcNow.AddHours(-2));

        var result = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.Equal("booking_in_past", result.Error!.Code);
        Assert.Equal(BookingStatus.Pending, (await Db.FindBookingAsync(booking.Id))!.Status);
        Assert.Empty(Calendar.Created);
    }

    [Fact]
    public async Task Slot_filled_in_google_meanwhile_reverts_to_pending_and_reports_calendar_busy()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Calendar.MarkBusy(TestTime.Slot(Day, 3)); // the psychologist added something there by hand

        var result = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.Equal("calendar_busy", result.Error!.Code);
        var saved = await Db.FindBookingAsync(booking.Id);
        Assert.Equal(BookingStatus.Pending, saved!.Status);
        Assert.Null(saved.DecidedAt);
        Assert.Null(saved.DecidedByUserId);
        Assert.Empty(Email.OfKind("Approved"));
    }

    [Fact]
    public async Task Google_failure_reverts_to_pending_and_rethrows()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Calendar.CreateThrows = new HttpRequestException("Google is down");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None));

        Assert.Equal(BookingStatus.Pending, (await Db.FindBookingAsync(booking.Id))!.Status);
        Assert.Empty(Email.OfKind("Approved"));
    }

    [Fact]
    public async Task After_a_failed_attempt_the_admin_can_approve_again()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Calendar.CreateThrows = new HttpRequestException("Google is down");
        await Assert.ThrowsAsync<HttpRequestException>(() => CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None));
        Calendar.CreateThrows = null;

        var retry = await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        Assert.True(retry.Succeeded);
        Assert.Single(Calendar.Created);
    }

    [Fact]
    public async Task Calendar_not_configured_reverts_to_pending_and_is_passed_on()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Calendar.NotConfigured = true;

        await Assert.ThrowsAsync<CalendarNotConfiguredException>(
            () => CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None));

        Assert.Equal(BookingStatus.Pending, (await Db.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Approved_slot_disappears_from_availability()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);
        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.DoesNotContain(slots, s => s.Start == TestTime.Slot(Day, 3));
    }
}

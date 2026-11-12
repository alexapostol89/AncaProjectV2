using api.Models;
using api.Services.Results;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>Step 5 — the admin rejects a request: the slot is freed and the client is told (with an optional reason).</summary>
public sealed class BookingRejectionTests : BookingServiceTestBase
{
    private async Task<(User Client, User Admin, Booking Booking)> PendingRequestAsync()
    {
        var client = await Db.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        var admin = await Db.AddUserAsync("Anca Admin", role: UserRole.Admin);
        var booking = await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 4));
        return (client, admin, booking);
    }

    [Fact]
    public async Task Reject_marks_the_booking_rejected_and_records_who_why_and_when()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        var result = await CreateService().RejectAsync(admin.Id, booking.Id, "  Sunt plecată în acea zi.  ", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal("Rejected", result.Value!.Status);
        var saved = await Db.FindBookingAsync(booking.Id);
        Assert.Equal(BookingStatus.Rejected, saved!.Status);
        Assert.Equal(admin.Id, saved.DecidedByUserId);
        Assert.NotNull(saved.DecidedAt);
        Assert.Equal("Sunt plecată în acea zi.", saved.RejectionReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_reason_is_stored_as_no_reason(string? reason)
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().RejectAsync(admin.Id, booking.Id, reason, CancellationToken.None);

        Assert.Null((await Db.FindBookingAsync(booking.Id))!.RejectionReason);
    }

    [Fact]
    public async Task Reject_emails_the_client_with_the_reason()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().RejectAsync(admin.Id, booking.Id, "Te rog alege altă zi.", CancellationToken.None);

        var email = Assert.Single(Email.OfKind("Rejected"));
        Assert.Equal("ioana@example.com", email.Email);
        Assert.Equal(TestTime.Slot(Day, 4), email.Start);
        Assert.Equal("Te rog alege altă zi.", email.Extra);
    }

    [Fact]
    public async Task Reject_never_touches_google_calendar()
    {
        var (_, admin, booking) = await PendingRequestAsync();

        await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.Empty(Calendar.Created);
        Assert.Empty(Email.OfKind("Approved"));
    }

    [Fact]
    public async Task Rejected_slot_becomes_available_again()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Assert.DoesNotContain(await CreateService().GetAvailabilityAsync(Day, CancellationToken.None), s => s.Start == TestTime.Slot(Day, 4));

        await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.Contains(await CreateService().GetAvailabilityAsync(Day, CancellationToken.None), s => s.Start == TestTime.Slot(Day, 4));
    }

    [Fact]
    public async Task Rejection_still_succeeds_when_the_email_fails()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        Email.ThrowOnRejected = true;

        var result = await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(BookingStatus.Rejected, (await Db.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Expired_request_is_rejected_without_emailing_the_client()
    {
        var client = await Db.AddUserAsync();
        var admin = await Db.AddUserAsync(role: UserRole.Admin);
        var booking = await Db.AddBookingAsync(client.Id, DateTimeOffset.UtcNow.AddDays(-1));

        var result = await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Empty(Email.OfKind("Rejected"));
    }

    [Fact]
    public async Task Approved_booking_cannot_be_rejected()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        await CreateService().ApproveAsync(admin.Id, booking.Id, CancellationToken.None);

        var result = await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.Equal("booking_not_pending", result.Error!.Code);
        Assert.Equal(BookingStatus.Approved, (await Db.FindBookingAsync(booking.Id))!.Status);
    }

    [Fact]
    public async Task Rejecting_twice_is_refused_and_emails_once()
    {
        var (_, admin, booking) = await PendingRequestAsync();
        await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        var second = await CreateService().RejectAsync(admin.Id, booking.Id, null, CancellationToken.None);

        Assert.Equal("booking_not_pending", second.Error!.Code);
        Assert.Single(Email.OfKind("Rejected"));
    }

    [Fact]
    public async Task Unknown_booking_returns_not_found()
    {
        var admin = await Db.AddUserAsync(role: UserRole.Admin);

        var result = await CreateService().RejectAsync(admin.Id, Guid.NewGuid(), null, CancellationToken.None);

        Assert.Equal(ServiceErrorType.NotFound, result.Error!.Type);
    }
}

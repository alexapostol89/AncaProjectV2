using api.Models;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>Step 3 — the client sees their own requests; the admin sees requests to review.</summary>
public sealed class BookingListTests : BookingServiceTestBase
{
    [Fact]
    public async Task Client_sees_only_their_own_requests_soonest_first()
    {
        var me = await Db.AddUserAsync("Me");
        var someoneElse = await Db.AddUserAsync("Someone Else");
        await Db.AddBookingAsync(me.Id, TestTime.Slot(Day, 5));
        await Db.AddBookingAsync(me.Id, TestTime.Slot(Day, 1), BookingStatus.Approved);
        await Db.AddBookingAsync(someoneElse.Id, TestTime.Slot(Day, 3));

        var mine = await CreateService().GetMineAsync(me.Id, CancellationToken.None);

        Assert.Equal(2, mine.Count);
        Assert.Equal(TestTime.Slot(Day, 1), mine[0].Start);
        Assert.Equal("Approved", mine[0].Status);
        Assert.Equal(TestTime.Slot(Day, 5), mine[1].Start);
        Assert.Equal("Pending", mine[1].Status);
    }

    [Fact]
    public async Task Client_list_hides_sessions_older_than_30_days()
    {
        var me = await Db.AddUserAsync();
        var old = TestTime.At(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-40)), 10, 0);
        var recent = TestTime.At(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), 10, 0);
        await Db.AddBookingAsync(me.Id, old, BookingStatus.Approved);
        await Db.AddBookingAsync(me.Id, recent, BookingStatus.Approved);

        var mine = await CreateService().GetMineAsync(me.Id, CancellationToken.None);

        Assert.Equal(recent, Assert.Single(mine).Start);
    }

    [Fact]
    public async Task Client_sees_the_rejection_reason()
    {
        var me = await Db.AddUserAsync();
        var admin = await Db.AddUserAsync("Admin", role: UserRole.Admin);
        var booking = await Db.AddBookingAsync(me.Id, TestTime.Slot(Day, 2));
        await CreateService().RejectAsync(admin.Id, booking.Id, "Sunt în concediu.", CancellationToken.None);

        var mine = await CreateService().GetMineAsync(me.Id, CancellationToken.None);

        Assert.Equal("Rejected", mine[0].Status);
        Assert.Equal("Sunt în concediu.", mine[0].RejectionReason);
    }

    [Fact]
    public async Task Returned_times_are_utc_instants()
    {
        var me = await Db.AddUserAsync();
        var start = TestTime.Slot(Day, 0);
        await Db.AddBookingAsync(me.Id, start);

        var mine = await CreateService().GetMineAsync(me.Id, CancellationToken.None);

        Assert.Equal(TimeSpan.Zero, mine[0].Start.Offset);
        Assert.Equal(start.UtcDateTime, mine[0].Start.UtcDateTime);
    }

    [Fact]
    public async Task Admin_pending_filter_shows_only_pending_soonest_first_with_client_details()
    {
        var client = await Db.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 8));
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 2));
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 4), BookingStatus.Approved);
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 5), BookingStatus.Rejected);

        var pending = await CreateService().GetForAdminAsync(BookingStatus.Pending, CancellationToken.None);

        Assert.Equal(2, pending.Count);
        Assert.All(pending, b => Assert.Equal("Pending", b.Status));
        Assert.Equal(TestTime.Slot(Day, 2), pending[0].Start);
        Assert.Equal("Ioana Ionescu", pending[0].ClientName);
        Assert.Equal("ioana@example.com", pending[0].ClientEmail);
    }

    [Theory]
    [InlineData(BookingStatus.Approved)]
    [InlineData(BookingStatus.Rejected)]
    public async Task Admin_status_filter_returns_only_that_status(BookingStatus status)
    {
        var client = await Db.AddUserAsync();
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 0), BookingStatus.Pending);
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 1), BookingStatus.Approved);
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 2), BookingStatus.Rejected);

        var list = await CreateService().GetForAdminAsync(status, CancellationToken.None);

        Assert.Equal(status.ToString(), Assert.Single(list).Status);
    }

    [Fact]
    public async Task Admin_without_filter_sees_everything_latest_first()
    {
        var client = await Db.AddUserAsync();
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 0), BookingStatus.Pending);
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 1), BookingStatus.Approved);
        await Db.AddBookingAsync(client.Id, TestTime.Slot(Day, 2), BookingStatus.Rejected);

        var all = await CreateService().GetForAdminAsync(null, CancellationToken.None);

        Assert.Equal(3, all.Count);
        Assert.Equal(TestTime.Slot(Day, 2), all[0].Start);
        Assert.Equal(TestTime.Slot(Day, 0), all[2].Start);
    }
}

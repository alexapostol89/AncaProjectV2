using api.Models;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>The number shown on the admin's bell: requests waiting for a decision.</summary>
public sealed class PendingCountTests : BookingServiceTestBase
{
    [Fact]
    public async Task No_requests_means_zero()
    {
        Assert.Equal(0, await CreateService().CountPendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Counts_only_pending_requests_from_all_clients()
    {
        var alice = await Db.AddUserAsync("Alice");
        var bob = await Db.AddUserAsync("Bob");
        await Db.AddBookingAsync(alice.Id, TestTime.Slot(Day, 0));
        await Db.AddBookingAsync(bob.Id, TestTime.Slot(Day, 1));
        await Db.AddBookingAsync(bob.Id, TestTime.Slot(Day, 2), BookingStatus.Approved);
        await Db.AddBookingAsync(bob.Id, TestTime.Slot(Day, 3), BookingStatus.Rejected);

        Assert.Equal(2, await CreateService().CountPendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Expired_pending_requests_still_count_until_the_admin_clears_them()
    {
        var client = await Db.AddUserAsync();
        await Db.AddBookingAsync(client.Id, DateTimeOffset.UtcNow.AddDays(-2));

        Assert.Equal(1, await CreateService().CountPendingAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Count_goes_up_on_request_and_down_on_approve_or_reject()
    {
        var client = await Db.AddUserAsync();
        var admin = await Db.AddUserAsync(role: UserRole.Admin);

        var first = await CreateService().RequestAsync(client.Id, TestTime.Slot(Day, 0), CancellationToken.None);
        var second = await CreateService().RequestAsync(client.Id, TestTime.Slot(Day, 1), CancellationToken.None);
        Assert.Equal(2, await CreateService().CountPendingAsync(CancellationToken.None));

        await CreateService().ApproveAsync(admin.Id, first.Value!.Id, CancellationToken.None);
        Assert.Equal(1, await CreateService().CountPendingAsync(CancellationToken.None));

        await CreateService().RejectAsync(admin.Id, second.Value!.Id, null, CancellationToken.None);
        Assert.Equal(0, await CreateService().CountPendingAsync(CancellationToken.None));
    }
}

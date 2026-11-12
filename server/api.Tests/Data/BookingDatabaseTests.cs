using api.Models;
using api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Data;

/// <summary>Rules enforced by the database itself (indexes and foreign keys in AppDbContext).</summary>
public sealed class BookingDatabaseTests : IDisposable
{
    private readonly SqliteDb _db = new();
    private readonly DateTimeOffset _start = TestTime.Slot(TestTime.FutureWorkday(), 0);

    [Theory]
    [InlineData(BookingStatus.Pending, BookingStatus.Pending)]
    [InlineData(BookingStatus.Pending, BookingStatus.Approved)]
    [InlineData(BookingStatus.Approved, BookingStatus.Pending)]
    public async Task Two_active_bookings_cannot_hold_the_same_start(BookingStatus first, BookingStatus second)
    {
        var a = await _db.AddUserAsync();
        var b = await _db.AddUserAsync();
        await _db.AddBookingAsync(a.Id, _start, first);

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.AddBookingAsync(b.Id, _start, second));
    }

    [Fact]
    public async Task Rejected_bookings_do_not_block_the_start()
    {
        var a = await _db.AddUserAsync();
        await _db.AddBookingAsync(a.Id, _start, BookingStatus.Rejected);
        await _db.AddBookingAsync(a.Id, _start, BookingStatus.Rejected);

        var active = await _db.AddBookingAsync(a.Id, _start, BookingStatus.Pending);

        Assert.NotEqual(Guid.Empty, active.Id);
    }

    [Fact]
    public async Task Deleting_a_client_deletes_their_bookings()
    {
        var client = await _db.AddUserAsync();
        var booking = await _db.AddBookingAsync(client.Id, _start);

        await using (var db = _db.CreateContext())
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == client.Id));
            await db.SaveChangesAsync();
        }

        Assert.Null(await _db.FindBookingAsync(booking.Id));
    }

    [Fact]
    public async Task Deleting_the_deciding_admin_keeps_the_booking()
    {
        var client = await _db.AddUserAsync();
        var admin = await _db.AddUserAsync(role: UserRole.Admin);
        var booking = await _db.AddBookingAsync(client.Id, _start, BookingStatus.Approved);
        await using (var db = _db.CreateContext())
        {
            (await db.Bookings.SingleAsync(b => b.Id == booking.Id)).DecidedByUserId = admin.Id;
            await db.SaveChangesAsync();
        }

        await using (var db = _db.CreateContext())
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == admin.Id));
            await db.SaveChangesAsync();
        }

        var saved = await _db.FindBookingAsync(booking.Id);
        Assert.NotNull(saved);
        Assert.Null(saved.DecidedByUserId);
        Assert.Equal(BookingStatus.Approved, saved.Status);
    }

    [Fact]
    public async Task Booking_must_belong_to_an_existing_user()
    {
        await Assert.ThrowsAsync<DbUpdateException>(() => _db.AddBookingAsync(Guid.NewGuid(), _start));
    }

    public void Dispose() => _db.Dispose();
}

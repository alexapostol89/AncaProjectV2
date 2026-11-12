using api.Models;
using api.Services;
using api.Tests.Infrastructure;

namespace api.Tests.Services;

/// <summary>Step 1 — the client looks at a day and sees which times can still be requested.</summary>
public sealed class BookingAvailabilityTests : BookingServiceTestBase
{
    [Fact]
    public async Task Empty_day_offers_all_twelve_50_minute_slots_from_10_to_20()
    {
        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.Equal(12, slots.Count);
        Assert.Equal(TestTime.At(Day, 10, 0), slots[0].Start);
        Assert.Equal(TestTime.At(Day, 20, 0), slots[^1].End);
        Assert.All(slots, s => Assert.Equal(TimeSpan.FromMinutes(50), s.End - s.Start));
    }

    [Fact]
    public async Task Sunday_has_no_slots()
    {
        var slots = await CreateService().GetAvailabilityAsync(TestTime.FutureSunday(), CancellationToken.None);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task Slot_busy_in_google_calendar_is_hidden()
    {
        var busy = TestTime.Slot(Day, 3);
        Calendar.MarkBusy(busy);

        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.Equal(11, slots.Count);
        Assert.DoesNotContain(slots, s => s.Start == busy);
    }

    [Fact]
    public async Task Pending_request_holds_its_slot_so_nobody_else_sees_it()
    {
        var other = await Db.AddUserAsync("Another Client");
        var held = TestTime.Slot(Day, 2);
        await Db.AddBookingAsync(other.Id, held, BookingStatus.Pending);

        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.DoesNotContain(slots, s => s.Start == held);
        Assert.Equal(11, slots.Count);
    }

    [Fact]
    public async Task Approved_booking_hides_its_slot()
    {
        var other = await Db.AddUserAsync();
        var taken = TestTime.Slot(Day, 5);
        await Db.AddBookingAsync(other.Id, taken, BookingStatus.Approved);

        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.DoesNotContain(slots, s => s.Start == taken);
    }

    [Fact]
    public async Task Rejected_request_frees_the_slot_again()
    {
        var other = await Db.AddUserAsync();
        var freed = TestTime.Slot(Day, 4);
        await Db.AddBookingAsync(other.Id, freed, BookingStatus.Rejected);

        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.Contains(slots, s => s.Start == freed);
        Assert.Equal(12, slots.Count);
    }

    [Fact]
    public async Task Bookings_on_other_days_do_not_affect_this_day()
    {
        var other = await Db.AddUserAsync();
        await Db.AddBookingAsync(other.Id, TestTime.Slot(Day.AddDays(1), 0));
        await Db.AddBookingAsync(other.Id, TestTime.Slot(Day.AddDays(-1), 0));

        var slots = await CreateService().GetAvailabilityAsync(Day, CancellationToken.None);

        Assert.Equal(12, slots.Count);
    }

    [Fact]
    public async Task Calendar_not_configured_is_passed_to_the_controller()
    {
        Calendar.NotConfigured = true;

        await Assert.ThrowsAsync<CalendarNotConfiguredException>(
            () => CreateService().GetAvailabilityAsync(Day, CancellationToken.None));
    }
}

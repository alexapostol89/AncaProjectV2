using api.Services;

namespace api.Tests.Infrastructure;

/// <summary>
/// Stand-in for Google Calendar. Produces the same slot grid as the real service
/// (Mon–Sat, 10:00–20:00 Romania time, 50 minutes) minus anything in <see cref="Busy"/>,
/// and records every event it is asked to create.
/// </summary>
public sealed class FakeCalendarService : IGoogleCalendarService
{
    public record CreatedEvent(string Name, string Email, DateTimeOffset Start, string EventId);

    /// <summary>Times already taken in the "Google" calendar.</summary>
    public List<CalendarSlot> Busy { get; } = [];

    public List<CreatedEvent> Created { get; } = [];

    /// <summary>Simulates missing or expired Google credentials.</summary>
    public bool NotConfigured { get; set; }

    /// <summary>If set, CreateBookingAsync throws this (e.g. a Google API outage).</summary>
    public Exception? CreateThrows { get; set; }

    /// <summary>If set, GetAvailabilityAsync throws this.</summary>
    public Exception? AvailabilityThrows { get; set; }

    public int SessionMinutes { get; set; } = 50;

    public void MarkBusy(DateTimeOffset start, int minutes = 50) => Busy.Add(new CalendarSlot(start, start.AddMinutes(minutes)));

    public Task<IReadOnlyList<CalendarSlot>> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken)
    {
        if (NotConfigured) throw new CalendarNotConfiguredException();
        if (AvailabilityThrows is not null) throw AvailabilityThrows;
        if (date.DayOfWeek == DayOfWeek.Sunday) return Task.FromResult<IReadOnlyList<CalendarSlot>>([]);

        var slots = new List<CalendarSlot>();
        for (var minutes = 600; minutes + SessionMinutes <= 1200; minutes += 60)
        {
            var start = TestTime.At(date, minutes / 60, minutes % 60);
            var end = start.AddMinutes(SessionMinutes);
            if (start <= DateTimeOffset.UtcNow || Busy.Any(b => start < b.End && end > b.Start)) continue;
            slots.Add(new CalendarSlot(start, end));
        }
        return Task.FromResult<IReadOnlyList<CalendarSlot>>(slots);
    }

    public Task<CalendarBookingEvent> CreateBookingAsync(string name, string email, DateTimeOffset start, CancellationToken cancellationToken)
    {
        if (NotConfigured) throw new CalendarNotConfiguredException();
        if (CreateThrows is not null) throw CreateThrows;
        var end = start.AddMinutes(SessionMinutes);
        if (Busy.Any(b => start < b.End && end > b.Start)) throw new CalendarSlotUnavailableException();

        var id = $"evt-{Created.Count + 1}";
        Created.Add(new CreatedEvent(name, email, start, id));
        MarkBusy(start, SessionMinutes);
        return Task.FromResult(new CalendarBookingEvent(id, $"https://meet.google.com/{id}"));
    }
}

using api.Configuration;
using api.Data;
using api.DTOs.Bookings;
using api.Models;
using api.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services;

public interface IBookingService
{
    /// <summary>Free slots on a day: free in Google Calendar and not held by a pending/approved request.</summary>
    Task<IReadOnlyList<CalendarSlot>> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken);

    /// <summary>Saves a Pending request and notifies the practice. Nothing goes into Google Calendar yet.</summary>
    Task<ServiceResult<MyBookingDto>> RequestAsync(Guid userId, DateTimeOffset start, CancellationToken cancellationToken);

    Task<IReadOnlyList<MyBookingDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>How many requests are waiting for a decision (the admin's bell).</summary>
    Task<int> CountPendingAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AdminBookingDto>> GetForAdminAsync(BookingStatus? status, CancellationToken cancellationToken, Guid? userId = null);

    /// <summary>Creates the Google Calendar event (which invites the client) and emails the client.</summary>
    Task<ServiceResult<AdminBookingDto>> ApproveAsync(Guid adminId, Guid bookingId, CancellationToken cancellationToken);

    /// <summary>Frees the slot and emails the client.</summary>
    Task<ServiceResult<AdminBookingDto>> RejectAsync(Guid adminId, Guid bookingId, string? reason, CancellationToken cancellationToken);
}

/// <summary>
/// Booking workflow: client requests a slot (Pending, slot held) → admin approves (Google event + emails)
/// or rejects (slot freed + email). Calendar errors (CalendarNotConfiguredException, etc.) are rethrown for the controller.
/// </summary>
public sealed class BookingService(
    AppDbContext db,
    IGoogleCalendarService calendar,
    IEmailService emailService,
    IOptions<ClientAppSettings> clientApp,
    ILogger<BookingService> logger) : IBookingService
{
    /// <summary>How many open requests one client may have at once (stops one account from holding the whole week).</summary>
    private const int MaxPendingPerUser = 3;

    private static readonly TimeZoneInfo RomaniaTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");

    public async Task<IReadOnlyList<CalendarSlot>> GetAvailabilityAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var slots = await calendar.GetAvailabilityAsync(date, cancellationToken);
        if (slots.Count == 0) return slots;

        var from = slots[0].Start.UtcDateTime;
        var to = slots[^1].End.UtcDateTime;
        var held = await db.Bookings
            .Where(b => b.Status != BookingStatus.Rejected && b.StartUtc < to && b.EndUtc > from)
            .Select(b => new { b.StartUtc, b.EndUtc })
            .ToListAsync(cancellationToken);

        return slots
            .Where(s => !held.Any(h => s.Start.UtcDateTime < h.EndUtc && s.End.UtcDateTime > h.StartUtc))
            .ToList();
    }

    public async Task<ServiceResult<MyBookingDto>> RequestAsync(Guid userId, DateTimeOffset start, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null)
            return ServiceResult<MyBookingDto>.Fail(ServiceErrorType.Unauthorized, "Contul nu mai există.", "user_not_found");

        var pending = await db.Bookings.CountAsync(
            b => b.UserId == userId && b.Status == BookingStatus.Pending && b.StartUtc > DateTime.UtcNow, cancellationToken);
        if (pending >= MaxPendingPerUser)
            return ServiceResult<MyBookingDto>.Fail(ServiceErrorType.Conflict,
                $"Ai deja {MaxPendingPerUser} cereri care așteaptă confirmarea.", "too_many_pending");

        // Re-check against Google Calendar and the other requests: the slot list on screen may be stale.
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(start, RomaniaTimeZone).DateTime);
        var available = await GetAvailabilityAsync(localDate, cancellationToken);
        var slot = available.FirstOrDefault(s => s.Start == start);
        if (slot is null) return SlotTaken();

        var booking = new Booking
        {
            UserId = userId,
            StartUtc = slot.Start.UtcDateTime,
            EndUtc = slot.End.UtcDateTime
        };
        db.Bookings.Add(booking);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Most likely someone else requested the same slot a moment earlier
            // (unique index on active StartUtc). Check instead of parsing provider-specific error codes.
            db.ChangeTracker.Clear();
            var held = await db.Bookings.AnyAsync(
                b => b.StartUtc == booking.StartUtc && b.Status != BookingStatus.Rejected, cancellationToken);
            if (held) return SlotTaken();
            throw;
        }

        var reviewLink = $"{clientApp.Value.BaseUrl.TrimEnd('/')}/admin/programari";
        try
        {
            await emailService.SendBookingRequestedAsync(user.FullName, user.Email, slot.Start, reviewLink, cancellationToken);
        }
        catch (Exception ex)
        {
            // The request is saved and visible in the admin page, so don't fail the client's request.
            logger.LogError(ex, "Booking {BookingId} saved, but the practice notification email failed.", booking.Id);
        }

        return ServiceResult<MyBookingDto>.Ok(ToMyDto(booking));
    }

    public async Task<IReadOnlyList<MyBookingDto>> GetMineAsync(Guid userId, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-30);
        var bookings = await db.Bookings
            .Where(b => b.UserId == userId && b.StartUtc > since)
            .OrderBy(b => b.StartUtc)
            .ToListAsync(cancellationToken);
        return bookings.Select(ToMyDto).ToList();
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken) =>
        db.Bookings.CountAsync(b => b.Status == BookingStatus.Pending, cancellationToken);

    public async Task<IReadOnlyList<AdminBookingDto>> GetForAdminAsync(BookingStatus? status, CancellationToken cancellationToken, Guid? userId = null)
    {
        var query = db.Bookings.Include(b => b.User).AsQueryable();
        if (status is { } s) query = query.Where(b => b.Status == s);
        if (userId is { } id) query = query.Where(b => b.UserId == id);

        // Pending: soonest first (what needs a decision next). Others: most recent first, capped.
        var bookings = status == BookingStatus.Pending
            ? await query.OrderBy(b => b.StartUtc).ToListAsync(cancellationToken)
            : await query.OrderByDescending(b => b.StartUtc).Take(200).ToListAsync(cancellationToken);
        return bookings.Select(ToAdminDto).ToList();
    }

    public async Task<ServiceResult<AdminBookingDto>> ApproveAsync(Guid adminId, Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Pending) return NotPending();
        if (booking.StartUtc <= DateTime.UtcNow)
            return ServiceResult<AdminBookingDto>.Fail(ServiceErrorType.Conflict,
                "Ora ședinței a trecut deja. Respinge cererea.", "booking_in_past");

        // Claim the booking first so two admins clicking "Approve" can't create two calendar events.
        var now = DateTime.UtcNow;
        var claimed = await db.Bookings
            .Where(b => b.Id == bookingId && b.Status == BookingStatus.Pending)
            .ExecuteUpdateAsync(u => u
                .SetProperty(b => b.Status, BookingStatus.Approved)
                .SetProperty(b => b.DecidedAt, now)
                .SetProperty(b => b.DecidedByUserId, adminId), cancellationToken);
        if (claimed == 0) return NotPending();

        string eventId;
        try
        {
            var start = Utc(booking.StartUtc);
            eventId = await calendar.CreateBookingAsync(booking.User.FullName, booking.User.Email, start, cancellationToken);
        }
        catch (Exception ex)
        {
            await db.Bookings
                .Where(b => b.Id == bookingId)
                .ExecuteUpdateAsync(u => u
                    .SetProperty(b => b.Status, BookingStatus.Pending)
                    .SetProperty(b => b.DecidedAt, (DateTime?)null)
                    .SetProperty(b => b.DecidedByUserId, (Guid?)null), CancellationToken.None);

            if (ex is CalendarSlotUnavailableException)
                return ServiceResult<AdminBookingDto>.Fail(ServiceErrorType.Conflict,
                    "Intervalul este deja ocupat în Google Calendar. Respinge cererea sau eliberează intervalul în calendar.", "calendar_busy");
            throw;
        }

        booking.Status = BookingStatus.Approved;
        booking.DecidedAt = now;
        booking.DecidedByUserId = adminId;
        booking.GoogleEventId = eventId;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendBookingApprovedAsync(booking.User.FullName, booking.User.Email,
                Utc(booking.StartUtc), cancellationToken);
        }
        catch (Exception ex)
        {
            // Google already sent the calendar invitation, so the client is still informed.
            logger.LogWarning(ex, "Booking {BookingId} approved, but the confirmation email failed.", booking.Id);
        }

        return ServiceResult<AdminBookingDto>.Ok(ToAdminDto(booking));
    }

    public async Task<ServiceResult<AdminBookingDto>> RejectAsync(Guid adminId, Guid bookingId, string? reason, CancellationToken cancellationToken)
    {
        var booking = await db.Bookings.Include(b => b.User).FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Pending) return NotPending();

        booking.Status = BookingStatus.Rejected;
        booking.DecidedAt = DateTime.UtcNow;
        booking.DecidedByUserId = adminId;
        booking.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await db.SaveChangesAsync(cancellationToken);

        // No point telling someone about a session whose time has already passed.
        if (booking.StartUtc > DateTime.UtcNow)
        {
            try
            {
                await emailService.SendBookingRejectedAsync(booking.User.FullName, booking.User.Email,
                    Utc(booking.StartUtc), booking.RejectionReason, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Booking {BookingId} rejected, but the email to the client failed.", booking.Id);
            }
        }

        return ServiceResult<AdminBookingDto>.Ok(ToAdminDto(booking));
    }

    private static ServiceResult<MyBookingDto> SlotTaken() =>
        ServiceResult<MyBookingDto>.Fail(ServiceErrorType.Conflict, "Acest interval nu mai este disponibil. Te rugăm să alegi altă oră.", "slot_taken");

    private static ServiceResult<AdminBookingDto> NotFound() =>
        ServiceResult<AdminBookingDto>.Fail(ServiceErrorType.NotFound, "Cererea nu a fost găsită.", "booking_not_found");

    private static ServiceResult<AdminBookingDto> NotPending() =>
        ServiceResult<AdminBookingDto>.Fail(ServiceErrorType.Conflict, "Cererea a fost deja procesată.", "booking_not_pending");

    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static MyBookingDto ToMyDto(Booking b) =>
        new(b.Id, Utc(b.StartUtc), Utc(b.EndUtc), b.Status.ToString(), b.RejectionReason, Utc(b.CreatedAt));

    private static AdminBookingDto ToAdminDto(Booking b) =>
        new(b.Id, Utc(b.StartUtc), Utc(b.EndUtc), b.Status.ToString(), b.User.FullName, b.User.Email,
            Utc(b.CreatedAt), b.DecidedAt is { } d ? Utc(d) : null, b.RejectionReason);
}

namespace api.Models;

/// <summary>Lifecycle of a session request. Stored as an int in the database.</summary>
public enum BookingStatus
{
    /// <summary>Requested by the client, waiting for the admin. The slot is held so nobody else can request it.</summary>
    Pending = 0,

    /// <summary>Approved by the admin: the Google Calendar event exists and the client was notified.</summary>
    Approved = 1,

    /// <summary>Declined by the admin. The slot is free again.</summary>
    Rejected = 2
}

/// <summary>
/// A session request ("Bookings" table). Created as Pending when a client picks a slot;
/// only when an admin approves it is the Google Calendar event created and the client emailed.
/// </summary>
public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The client who asked for the session. Deleting the user deletes their bookings.</summary>
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    /// <summary>Session start/end in UTC.</summary>
    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When the admin approved or rejected it.</summary>
    public DateTime? DecidedAt { get; set; }

    /// <summary>Admin who approved or rejected it (null if that admin was deleted).</summary>
    public Guid? DecidedByUserId { get; set; }

    /// <summary>Optional reason shown to the client when a request is rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Id of the Google Calendar event, set on approval.</summary>
    public string? GoogleEventId { get; set; }
}

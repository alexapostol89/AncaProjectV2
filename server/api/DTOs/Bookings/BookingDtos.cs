using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Bookings;

public sealed class BookingRequest
{
    [Required] public DateTimeOffset Start { get; set; }
}

public sealed class RejectBookingRequest
{
    /// <summary>Optional; emailed to the client.</summary>
    [StringLength(500)]
    public string? Reason { get; set; }
}

/// <summary>A booking as the client sees it (GET /api/bookings/mine).</summary>
public sealed record MyBookingDto(Guid Id, DateTimeOffset Start, DateTimeOffset End, string Status, string? RejectionReason, DateTimeOffset CreatedAt);

/// <summary>A booking as the admin sees it (GET /api/admin/bookings).</summary>
public sealed record AdminBookingDto(
    Guid Id,
    DateTimeOffset Start,
    DateTimeOffset End,
    string Status,
    string ClientName,
    string ClientEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DecidedAt,
    string? RejectionReason);

using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Admin;

/// <summary>Query string for GET /api/admin/users.</summary>
public class UserListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    /// <summary>Matches email or full name (case-insensitive).</summary>
    [StringLength(100)]
    public string? Search { get; set; }
}

/// <summary>A user as the admin sees them: profile plus how many sessions they have.</summary>
public class AdminUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }

    /// <summary>Requests waiting for approval.</summary>
    public int PendingBookings { get; init; }

    /// <summary>Approved sessions (past and upcoming).</summary>
    public int ApprovedBookings { get; init; }
}

namespace api.Models;

/// <summary>
/// A user account — the only table the login system needs ("Users").
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Always stored lowercase and trimmed; unique.</summary>
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Salted PBKDF2 hash made by ASP.NET's PasswordHasher — never the plain password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Foreign key to Roles.Id (1 = Admin, 2 = User).</summary>
    public UserRole RoleId { get; set; } = UserRole.User;

    /// <summary>Navigation property: the Role row this user points to.</summary>
    public Role Role { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>SHA-256 hash of the "forgot password" token emailed to the user (null when none pending).</summary>
    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetTokenExpiresAt { get; set; }
}

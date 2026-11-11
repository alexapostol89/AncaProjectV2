using api.Models;

namespace api.DTOs.Auth;

/// <summary>
/// Returned by register and login. The frontend stores Token and sends it on
/// every request as "Authorization: Bearer {Token}" until ExpiresAt.
/// Logging out = the frontend simply deletes the token.
/// </summary>
public class AuthResponse
{
    public string Token { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }

    public UserDto User { get; init; } = new();
}

/// <summary>Public view of a user. Never return the User entity itself (it contains the password hash).</summary>
public class UserDto
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? LastLoginAt { get; init; }

    public static UserDto FromUser(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.RoleId.ToString(),
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}

using api.Models;

namespace api.Services;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    /// <summary>Creates a signed JWT carrying the user's id, email, name and role.</summary>
    AccessToken CreateToken(User user);

    /// <summary>Random, URL-safe token for "forgot password" links.</summary>
    string GenerateResetToken();

    /// <summary>SHA-256 hash of a reset token — only the hash is stored in the database.</summary>
    string HashToken(string token);
}

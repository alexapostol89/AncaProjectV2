using api.Models;

namespace api.Services;

public record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    /// <summary>Creates a signed JWT carrying the user's id, email, name and role.</summary>
    AccessToken CreateToken(User user);
}

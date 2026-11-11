using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using api.Configuration;
using api.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace api.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly SigningCredentials _signingCredentials;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddHours(_settings.ExpiryHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.Subject, user.Id.ToString()),
                new Claim(AppClaimTypes.Email, user.Email),
                new Claim(AppClaimTypes.Name, user.FullName),
                new Claim(AppClaimTypes.Role, user.RoleId.ToString())
            ]),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            SigningCredentials = _signingCredentials
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAt);
    }

    public string GenerateResetToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

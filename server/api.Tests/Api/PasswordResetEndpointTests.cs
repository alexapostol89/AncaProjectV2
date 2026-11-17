using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using api.Configuration;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Models;
using api.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace api.Tests.Api;

/// <summary>"Forgot password": request a link by email, then set a new password with it.</summary>
public sealed class PasswordResetEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private const string NewPassword = "NouaParola9";

    /// <summary>Asks for a reset link and returns the email + token from the emailed link.</summary>
    private async Task<(string Email, string Token)> RequestLinkAsync(string email)
    {
        var response = await _api.Anonymous().PostAsJsonAsync("/api/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var link = new Uri(_api.Email.OfKind("PasswordReset").Last().Extra!);
        var query = QueryHelpers.ParseQuery(link.Query);
        return (query["email"].ToString(), query["token"].ToString());
    }

    private Task<HttpResponseMessage> ResetAsync(string email, string token, string password = NewPassword) =>
        _api.Anonymous().PostAsJsonAsync("/api/auth/reset-password", new { email, token, newPassword = password });

    private Task<HttpResponseMessage> LoginAsync(string email, string password) =>
        _api.Anonymous().PostAsJsonAsync("/api/auth/login", new { email, password });

    [Fact]
    public async Task Known_email_gets_a_link_to_the_frontend_reset_page()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");

        await RequestLinkAsync("  MARIA@example.com ");

        var sent = Assert.Single(_api.Email.OfKind("PasswordReset"));
        Assert.Equal(user.Email, sent.Email);
        Assert.StartsWith("https://site.test/reset-password?", sent.Extra);
    }

    [Fact]
    public async Task Unknown_email_gets_the_same_answer_and_no_email()
    {
        await _api.AddUserAsync(email: "maria@example.com");
        var anonymous = _api.Anonymous();

        var known = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = "maria@example.com" });
        var unknown = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = "nobody@example.com" });

        Assert.Equal(HttpStatusCode.OK, unknown.StatusCode);
        Assert.Equal((await ApiFactory.ReadAsync<MessageResponse>(known))!.Message,
            (await ApiFactory.ReadAsync<MessageResponse>(unknown))!.Message);
        Assert.Single(_api.Email.OfKind("PasswordReset"));
    }

    [Fact]
    public async Task Only_the_token_hash_is_stored()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");

        var (_, token) = await RequestLinkAsync(user.Email);

        await using var db = _api.CreateDbContext();
        var saved = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        Assert.NotNull(saved.PasswordResetTokenHash);
        Assert.NotEqual(token, saved.PasswordResetTokenHash);
    }

    [Fact]
    public async Task Valid_link_sets_the_new_password()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, token) = await RequestLinkAsync(user.Email);

        var response = await ResetAsync(email, token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(user.Email, NewPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(user.Email, "Parola123!")).StatusCode);
    }

    [Fact]
    public async Task Link_works_only_once()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, token) = await RequestLinkAsync(user.Email);
        await ResetAsync(email, token);

        var second = await ResetAsync(email, token, "AltaParola7");

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal("invalid_link", (await ApiFactory.ReadAsync<ErrorResponse>(second))!.Code);
    }

    [Fact]
    public async Task Requesting_a_new_link_cancels_the_old_one()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, oldToken) = await RequestLinkAsync(user.Email);
        await RequestLinkAsync(user.Email);

        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(email, oldToken)).StatusCode);
    }

    [Fact]
    public async Task Expired_link_is_rejected()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, token) = await RequestLinkAsync(user.Email);
        await using (var db = _api.CreateDbContext())
        {
            var saved = await db.Users.SingleAsync(u => u.Id == user.Id);
            saved.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var response = await ResetAsync(email, token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_link", (await ApiFactory.ReadAsync<ErrorResponse>(response))!.Code);
    }

    [Fact]
    public async Task Wrong_token_or_other_users_email_is_rejected()
    {
        var maria = await _api.AddUserAsync(email: "maria@example.com");
        var ioana = await _api.AddUserAsync("Ioana Ionescu", email: "ioana@example.com");
        var (email, token) = await RequestLinkAsync(maria.Email);

        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(email, token + "x")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(ioana.Email, token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, token)).StatusCode); // the real link still works
    }

    [Fact]
    public async Task Weak_new_password_is_rejected_and_the_link_stays_usable()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, token) = await RequestLinkAsync(user.Email);

        Assert.Equal(HttpStatusCode.BadRequest, (await ResetAsync(email, token, "weak")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await ResetAsync(email, token)).StatusCode);
    }

    // ---- Old logins stop working after a reset ---------------------------------------

    [Fact]
    public async Task Reset_logs_out_every_existing_login()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var oldSession = _api.ClientFor(user); // e.g. someone who knew the old password
        Assert.Equal(HttpStatusCode.OK, (await oldSession.GetAsync("/api/auth/me")).StatusCode);

        var (email, token) = await RequestLinkAsync(user.Email);
        await ResetAsync(email, token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await oldSession.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Logging_in_with_the_new_password_gives_a_working_token()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var (email, token) = await RequestLinkAsync(user.Email);
        await ResetAsync(email, token);

        var auth = await ApiFactory.ReadAsync<AuthResponse>(await LoginAsync(user.Email, NewPassword));
        var client = _api.Anonymous();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Reset_does_not_log_out_other_users()
    {
        var maria = await _api.AddUserAsync(email: "maria@example.com");
        var ioana = await _api.AddUserAsync("Ioana Ionescu", email: "ioana@example.com");
        var ioanaSession = _api.ClientFor(ioana);

        var (email, token) = await RequestLinkAsync(maria.Email);
        await ResetAsync(email, token);

        Assert.Equal(HttpStatusCode.OK, (await ioanaSession.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Tokens_issued_before_this_check_existed_keep_working_until_a_reset()
    {
        var user = await _api.AddUserAsync(email: "maria@example.com");
        var legacy = _api.Anonymous();
        legacy.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", LegacyTokenFor(user));

        Assert.Equal(HttpStatusCode.OK, (await legacy.GetAsync("/api/auth/me")).StatusCode);

        var (email, token) = await RequestLinkAsync(user.Email);
        await ResetAsync(email, token);

        Assert.Equal(HttpStatusCode.Unauthorized, (await legacy.GetAsync("/api/auth/me")).StatusCode);
    }

    /// <summary>A token like the ones issued before the "tv" claim was added (no token version in it).</summary>
    private string LegacyTokenFor(User user)
    {
        var jwt = _api.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.Subject, user.Id.ToString()),
                new Claim(AppClaimTypes.Email, user.Email),
                new Claim(AppClaimTypes.Name, user.FullName),
                new Claim(AppClaimTypes.Role, user.RoleId.ToString())
            ]),
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256)
        });
    }

    public void Dispose() => _api.Dispose();
}

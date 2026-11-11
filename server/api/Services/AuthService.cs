using System.Security.Cryptography;
using System.Text;
using api.Configuration;
using api.Data;
using api.DTOs.Auth;
using api.Models;
using api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services;

public class AuthService : IAuthService
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly ClientAppSettings _clientApp;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        IEmailService emailService,
        IOptions<ClientAppSettings> clientApp,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailService = emailService;
        _clientApp = clientApp.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var email = NormalizeEmail(request.Email);

        if (await _db.Users.AnyAsync(u => u.Email == email))
        {
            return ServiceResult<AuthResponse>.Fail(ServiceErrorType.Conflict,
                "Există deja un cont cu această adresă de email.", "email_taken");
        }

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            RoleId = UserRole.User, // public registration never creates admins
            LastLoginAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Two sign-ups with the same email at the same moment: the unique index on Email stops the second one.
            return ServiceResult<AuthResponse>.Fail(ServiceErrorType.Conflict,
                "Există deja un cont cu această adresă de email.", "email_taken");
        }

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Same message for "no such email" and "wrong password", so the login
        // form can't be used to find out who has an account.
        if (user is null || !VerifyPassword(user, request.Password))
        {
            return ServiceResult<AuthResponse>.Fail(ServiceErrorType.Unauthorized,
                "Email sau parolă incorectă.", "invalid_credentials");
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult<AuthResponse>.Ok(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user is null
            ? ServiceResult<UserDto>.Fail(ServiceErrorType.NotFound, "Contul nu a fost găsit.", "not_found")
            : ServiceResult<UserDto>.Ok(UserDto.FromUser(user));
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
        if (user is null)
        {
            return; // say nothing — the controller always returns the same message
        }

        // The raw token goes in the email; only its hash is saved.
        var token = _tokenService.GenerateResetToken();
        user.PasswordResetTokenHash = _tokenService.HashToken(token);
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.Add(ResetTokenLifetime);
        await _db.SaveChangesAsync(cancellationToken);

        var link = QueryHelpers.AddQueryString($"{_clientApp.BaseUrl.TrimEnd('/')}/reset-password",
            new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = token });

        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.FullName, link, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email.");
        }
    }

    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email);

        var tokenIsValid = user?.PasswordResetTokenHash is not null
            && user.PasswordResetTokenExpiresAt > DateTime.UtcNow
            && CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(user.PasswordResetTokenHash),
                Encoding.UTF8.GetBytes(_tokenService.HashToken(request.Token)));

        if (!tokenIsValid)
        {
            return ServiceResult.Fail(ServiceErrorType.Validation,
                "Linkul este invalid sau a expirat. Te rugăm să soliciți unul nou.", "invalid_link");
        }

        user!.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.PasswordResetTokenHash = null; // a link works only once
        user.PasswordResetTokenExpiresAt = null;
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "Contul nu a fost găsit.", "not_found");
        }

        if (!VerifyPassword(user, request.CurrentPassword))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "Parola curentă este incorectă.", "wrong_password");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAccountAsync(Guid userId, DeleteAccountRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail(ServiceErrorType.NotFound, "Contul nu a fost găsit.", "not_found");
        }

        if (!VerifyPassword(user, request.Password))
        {
            return ServiceResult.Fail(ServiceErrorType.Validation, "Parola este incorectă.", "wrong_password");
        }

        if (user.RoleId == UserRole.Admin && await _db.Users.CountAsync(u => u.RoleId == UserRole.Admin) == 1)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict, "Nu poți șterge singurul cont de administrator.", "last_admin");
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    // ---- Helpers -----------------------------------------------------------------

    private bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);

        // Hash made with older settings: upgrade it now that we know the password.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, password);
        }

        return result != PasswordVerificationResult.Failed;
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = _tokenService.CreateToken(user);
        return new AuthResponse { Token = token.Token, ExpiresAt = token.ExpiresAt, User = UserDto.FromUser(user) };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

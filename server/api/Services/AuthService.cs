using api.Data;
using api.DTOs.Auth;
using api.Models;
using api.Services.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
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

    public async Task<ServiceResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return ServiceResult<UserDto>.Fail(ServiceErrorType.NotFound, "Contul nu a fost găsit.", "not_found");
        }

        user.FullName = request.FullName.Trim();
        await _db.SaveChangesAsync();
        return ServiceResult<UserDto>.Ok(UserDto.FromUser(user));
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

        // Admin accounts are managed by the developers, never deleted from the website.
        if (user.RoleId == UserRole.Admin)
        {
            return ServiceResult.Fail(ServiceErrorType.Conflict,
                "Contul de administrator nu poate fi șters din site. Contactează echipa tehnică.", "admin_account");
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

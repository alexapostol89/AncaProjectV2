using api.DTOs.Auth;
using api.Services.Results;

namespace api.Services;

public interface IAuthService
{
    /// <summary>Creates a normal User account and logs it in straight away.</summary>
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);

    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId);

    /// <summary>Emails a reset link if the account exists. Never reveals whether it does.</summary>
    Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);

    Task<ServiceResult> ResetPasswordAsync(ResetPasswordRequest request);

    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>Permanently deletes the account (GDPR right to erasure).</summary>
    Task<ServiceResult> DeleteAccountAsync(Guid userId, DeleteAccountRequest request);
}

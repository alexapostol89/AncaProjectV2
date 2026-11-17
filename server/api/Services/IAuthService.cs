using api.DTOs.Auth;
using api.Services.Results;

namespace api.Services;

public interface IAuthService
{
    /// <summary>Creates a normal User account and logs it in straight away.</summary>
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);

    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);

    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId);

    /// <summary>Updates the logged-in user's own profile (name).</summary>
    Task<ServiceResult<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

    /// <summary>Permanently deletes the account (GDPR right to erasure).</summary>
    Task<ServiceResult> DeleteAccountAsync(Guid userId, DeleteAccountRequest request);
}

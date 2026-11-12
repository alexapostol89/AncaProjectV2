using api.Configuration;
using api.DTOs.Auth;
using api.DTOs.Common;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

/// <summary>
/// Registration, login and account management for site clients.
/// register/login return { token, expiresAt, user }; the frontend sends the
/// token as "Authorization: Bearer {token}". Logout = delete the token client-side.
/// </summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Creates a client account (role User) and logs it in.</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }

    /// <summary>The logged-in user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetCurrentUserAsync(CurrentUserId);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }

    /// <summary>Updates the logged-in user's name. Returns the updated profile.</summary>
    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var result = await _authService.UpdateProfileAsync(CurrentUserId, request);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }

    /// <summary>Emails a password reset link. Same answer whether or not the email exists.</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.ForgotPasswordAsync(request.Email, cancellationToken);
        return Ok(new MessageResponse(
            "Dacă există un cont cu această adresă, ți-am trimis un email cu instrucțiuni de resetare a parolei."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        return result.Succeeded
            ? Ok(new MessageResponse("Parola a fost schimbată. Te poți autentifica acum cu noua parolă."))
            : ErrorResult(result.Error!);
    }

    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _authService.ChangePasswordAsync(CurrentUserId, request);
        return result.Succeeded
            ? Ok(new MessageResponse("Parola a fost schimbată."))
            : ErrorResult(result.Error!);
    }

    /// <summary>Permanently deletes the logged-in user's account (GDPR right to erasure).</summary>
    [Authorize]
    [HttpPost("delete-account")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        var result = await _authService.DeleteAccountAsync(CurrentUserId, request);
        return result.Succeeded ? NoContent() : ErrorResult(result.Error!);
    }
}

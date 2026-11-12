using System.ComponentModel.DataAnnotations;

namespace api.DTOs.Auth;

// Request payloads for /api/auth. [ApiController] validates them automatically
// and returns 400 + ValidationProblemDetails, same as ContactRequest.

/// <summary>Password rules, defined once and reused by every request that sets a password.</summary>
public static class PasswordRules
{
    public const int MinLength = 8;

    /// <summary>At least one lowercase letter, one uppercase letter and one digit.</summary>
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$";

    public const string Message = "Parola trebuie să aibă cel puțin 8 caractere și să conțină o literă mică, o literă mare și o cifră.";
}

public class RegisterRequest
{
    [Required(ErrorMessage = "Numele este obligatoriu.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Numele trebuie să aibă între 2 și 120 de caractere.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adresa de email este obligatorie.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola este obligatorie.")]
    [StringLength(128, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.Message)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required(ErrorMessage = "Adresa de email este obligatorie.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola este obligatorie.")]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Adresa de email este obligatorie.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;
}

/// <summary>Sent by the frontend's /reset-password page with the email + token from the link.</summary>
public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola nouă este obligatorie.")]
    [StringLength(128, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.Message)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Parola curentă este obligatorie.")]
    [StringLength(128)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola nouă este obligatorie.")]
    [StringLength(128, MinimumLength = PasswordRules.MinLength, ErrorMessage = PasswordRules.Message)]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Fields a user may change on their own profile. Email is the login, so it stays fixed.</summary>
public class UpdateProfileRequest
{
    [Required(ErrorMessage = "Numele este obligatoriu.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Numele trebuie să aibă între 2 și 120 de caractere.")]
    public string FullName { get; set; } = string.Empty;
}

/// <summary>Account deletion requires re-entering the password.</summary>
public class DeleteAccountRequest
{
    [Required(ErrorMessage = "Parola este obligatorie.")]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;
}

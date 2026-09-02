using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

/// <summary>
/// Payload submitted from the public contact form.
/// Every rule here is re-validated server-side, regardless of what the
/// frontend already checked — the frontend validation is only a UX nicety.
/// </summary>
public class ContactRequest
{
    [Required(ErrorMessage = "Numele este obligatoriu.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Numele trebuie să aibă între 2 și 120 de caractere.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adresa de email este obligatorie.")]
    [EmailAddress(ErrorMessage = "Adresa de email nu este validă.")]
    [StringLength(180)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Numărul de telefon nu este valid.")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Te rugăm să alegi tipul de ședință preferat.")]
    [StringLength(80)]
    public string SessionType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mesajul este obligatoriu.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mesajul trebuie să aibă între 10 și 2000 de caractere.")]
    public string Message { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Este necesar acordul pentru prelucrarea datelor.")]
    public bool Consent { get; set; }

    /// <summary>
    /// Honeypot anti-spam field. Real users never see or fill this input
    /// (hidden via CSS in the form); bots that auto-fill every field will.
    /// If this has a value, the request is silently dropped.
    /// </summary>
    public string? Website { get; set; }
}

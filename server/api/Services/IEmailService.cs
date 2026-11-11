using api.DTOs;

namespace api.Services;

public interface IEmailService
{
    /// <summary>
    /// Sends the internal notification email (to the psychologist) and the
    /// confirmation email (to the visitor). Throws on failure so the
    /// controller can translate it into a proper HTTP error response.
    /// </summary>
    Task SendContactNotificationAsync(ContactRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sends the "reset your password" link.</summary>
    Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink, CancellationToken cancellationToken = default);
}

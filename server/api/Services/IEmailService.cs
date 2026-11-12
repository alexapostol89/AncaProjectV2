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

    /// <summary>Tells the practice a session request needs approval (throws on failure) and acknowledges it to the client (logged only).</summary>
    Task SendBookingRequestedAsync(string name, string email, DateTimeOffset start, string reviewLink, CancellationToken cancellationToken = default);

    /// <summary>Tells the client their session was approved.</summary>
    Task SendBookingApprovedAsync(string name, string email, DateTimeOffset start, CancellationToken cancellationToken = default);

    /// <summary>Tells the client their session request was declined, with an optional reason.</summary>
    Task SendBookingRejectedAsync(string name, string email, DateTimeOffset start, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Sends the "reset your password" link.</summary>
    Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink, CancellationToken cancellationToken = default);
}

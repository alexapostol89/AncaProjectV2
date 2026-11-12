using api.DTOs;
using api.Services;

namespace api.Tests.Infrastructure;

/// <summary>Records every email the app tries to send. Set the Throw* flags to simulate Resend failures.</summary>
public sealed class FakeEmailService : IEmailService
{
    public record Sent(string Kind, string Name, string Email, DateTimeOffset? Start = null, string? Extra = null);

    public List<Sent> Emails { get; } = [];

    public bool ThrowOnRequested { get; set; }
    public bool ThrowOnApproved { get; set; }
    public bool ThrowOnRejected { get; set; }

    public List<Sent> OfKind(string kind) => Emails.Where(e => e.Kind == kind).ToList();

    public Task SendContactNotificationAsync(ContactRequest request, CancellationToken cancellationToken = default)
    {
        Emails.Add(new Sent("Contact", request.Name, request.Email));
        return Task.CompletedTask;
    }

    public Task SendBookingRequestedAsync(string name, string email, DateTimeOffset start, string reviewLink, CancellationToken cancellationToken = default)
    {
        if (ThrowOnRequested) throw new InvalidOperationException("Email provider returned 500.");
        Emails.Add(new Sent("Requested", name, email, start, reviewLink));
        return Task.CompletedTask;
    }

    public Task SendBookingApprovedAsync(string name, string email, DateTimeOffset start, CancellationToken cancellationToken = default)
    {
        if (ThrowOnApproved) throw new InvalidOperationException("Email provider returned 500.");
        Emails.Add(new Sent("Approved", name, email, start));
        return Task.CompletedTask;
    }

    public Task SendBookingRejectedAsync(string name, string email, DateTimeOffset start, string? reason, CancellationToken cancellationToken = default)
    {
        if (ThrowOnRejected) throw new InvalidOperationException("Email provider returned 500.");
        Emails.Add(new Sent("Rejected", name, email, start, reason));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string toEmail, string fullName, string resetLink, CancellationToken cancellationToken = default)
    {
        Emails.Add(new Sent("PasswordReset", fullName, toEmail, Extra: resetLink));
        return Task.CompletedTask;
    }
}

using api.Configuration;
using api.DTOs;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Contact)]
public class ContactController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<ContactController> _logger;

    public ContactController(IEmailService emailService, ILogger<ContactController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Receives a contact form submission from the public website and
    /// forwards it by email to the psychologist's inbox.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(16 * 1024)] // 16 KB — a contact message never legitimately needs more
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Submit([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        // [ApiController] already runs model validation and returns 400 with
        // ValidationProblemDetails automatically for anything that fails the
        // DataAnnotations on ContactRequest, so we don't repeat that here.

        // Honeypot: bots fill every field, real visitors never see this one.
        if (!string.IsNullOrWhiteSpace(request.Website))
        {
            _logger.LogInformation("Contact submission dropped (honeypot triggered).");
            // Return success so bots don't learn the field is being checked.
            return Ok(new { message = "Mesajul a fost trimis." });
        }

        try
        {
            await _emailService.SendContactNotificationAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Do not log request.Message / personal content — only the fact
            // that delivery failed, for operational monitoring.
            _logger.LogError(ex, "Failed to deliver contact form submission.");
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Nu am putut trimite mesajul din cauza unei erori tehnice. Te rugăm să încerci din nou sau să ne scrii direct pe email."
            });
        }

        return Ok(new
        {
            message = "Mulțumesc pentru mesaj. Am primit solicitarea ta și voi reveni cât mai curând."
        });
    }
}

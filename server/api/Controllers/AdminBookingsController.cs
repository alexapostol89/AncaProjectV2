using api.DTOs.Bookings;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

/// <summary>Admin review of session requests: list, approve (creates the Google Calendar event), reject.</summary>
[Route("api/admin/bookings")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminBookingsController(IBookingService bookings, ILogger<AdminBookingsController> logger) : ApiControllerBase
{
    /// <summary>?status=Pending|Approved|Rejected (omit for all, latest 200) and optional ?userId= for one client.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] BookingStatus? status, [FromQuery] Guid? userId, CancellationToken cancellationToken) =>
        Ok(await bookings.GetForAdminAsync(status, cancellationToken, userId));

    /// <summary>Number of requests waiting for approval; polled by the bell in the navbar.</summary>
    [HttpGet("pending-count")]
    public async Task<IActionResult> PendingCount(CancellationToken cancellationToken) =>
        Ok(new { count = await bookings.CountPendingAsync(cancellationToken) });

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await bookings.ApproveAsync(CurrentUserId, id, cancellationToken);
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
        }
        catch (CalendarNotConfiguredException)
        {
            return StatusCode(503, new { message = "Google Calendar nu este conectat (credențiale lipsă sau expirate).", code = "calendar_not_configured" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not approve booking {BookingId}.", id);
            return StatusCode(502, new { message = "Evenimentul nu a putut fi creat în Google Calendar. Încearcă din nou.", code = "calendar_unavailable" });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBookingRequest request, CancellationToken cancellationToken)
    {
        var result = await bookings.RejectAsync(CurrentUserId, id, request.Reason, cancellationToken);
        return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
    }
}

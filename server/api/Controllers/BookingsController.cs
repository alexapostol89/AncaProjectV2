using api.Configuration;
using api.DTOs.Bookings;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

/// <summary>
/// Client side of booking. POST only creates a Pending request; the Google Calendar event
/// and the confirmation emails happen when an admin approves it (AdminBookingsController).
/// </summary>
[Route("api/bookings")]
[Authorize(Roles = nameof(UserRole.User))] // clients only; the admin uses /api/admin/bookings
[EnableRateLimiting(RateLimitPolicies.Booking)]
public sealed class BookingsController(IBookingService bookings, ILogger<BookingsController> logger) : ApiControllerBase
{
    internal const string NotConfiguredMessage = "Online booking is being set up. Please contact the practice directly.";

    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow) || date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)))
            return BadRequest(new { message = "Choose a date within the next 90 days." });
        try
        {
            var slots = await bookings.GetAvailabilityAsync(date, cancellationToken);
            return Ok(new { slots = slots.Select(s => new { start = s.Start, end = s.End }) });
        }
        catch (CalendarNotConfiguredException)
        {
            return StatusCode(503, new { message = NotConfiguredMessage, code = "calendar_not_configured" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not retrieve calendar availability.");
            return StatusCode(502, new { message = "Calendar availability is temporarily unavailable.", code = "calendar_unavailable" });
        }
    }

    [HttpGet("mine")]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken) =>
        Ok(await bookings.GetMineAsync(CurrentUserId, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BookingRequest request, CancellationToken cancellationToken)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");
        var local = TimeZoneInfo.ConvertTime(request.Start, zone);
        var minuteOfDay = local.Hour * 60 + local.Minute;
        if (local.DayOfWeek == DayOfWeek.Sunday || request.Start <= DateTimeOffset.UtcNow || minuteOfDay < 600 || minuteOfDay + 50 > 1200 || (minuteOfDay - 600) % 50 != 0 || local.Second != 0 || request.Start > DateTimeOffset.UtcNow.AddDays(90))
            return BadRequest(new { message = "Choose an available time between 10:00 and 20:00 Romania time, within the next 90 days.", code = "invalid_time" });
        try
        {
            var result = await bookings.RequestAsync(CurrentUserId, request.Start, cancellationToken);
            return result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error!);
        }
        catch (CalendarNotConfiguredException)
        {
            return StatusCode(503, new { message = NotConfiguredMessage, code = "calendar_not_configured" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create booking request.");
            return StatusCode(502, new { message = "The booking request could not be sent. Please try again.", code = "booking_failed" });
        }
    }
}

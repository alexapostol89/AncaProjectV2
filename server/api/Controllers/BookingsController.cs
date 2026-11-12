using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using api.Configuration;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Booking)]
public sealed class BookingsController(IGoogleCalendarService calendar, IEmailService emailService, ILogger<BookingsController> logger) : ControllerBase
{
    [HttpGet("availability")]
    public async Task<IActionResult> Availability([FromQuery] DateOnly date, CancellationToken cancellationToken)
    {
        if (date < DateOnly.FromDateTime(DateTime.UtcNow) || date > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)))
            return BadRequest(new { message = "Choose a date within the next 90 days." });
        try
        {
            var slots = await calendar.GetAvailabilityAsync(date, cancellationToken);
            return Ok(new { slots = slots.Select(s => new { start = s.Start, end = s.End }) });
        }
        catch (CalendarNotConfiguredException)
        {
            return StatusCode(503, new { message = "Online booking is being set up. Please contact the practice directly." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not retrieve calendar availability.");
            return StatusCode(502, new { message = "Calendar availability is temporarily unavailable." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Book([FromBody] BookingRequest request, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(AppClaimTypes.Email);
        var name = User.FindFirstValue(AppClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name)) return Unauthorized();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "GTB Standard Time" : "Europe/Bucharest");
        var local = TimeZoneInfo.ConvertTime(request.Start, zone);
        var minuteOfDay = local.Hour * 60 + local.Minute;
        if (local.DayOfWeek == DayOfWeek.Sunday || request.Start <= DateTimeOffset.UtcNow || minuteOfDay < 600 || minuteOfDay + 50 > 1200 || (minuteOfDay - 600) % 50 != 0 || local.Second != 0 || request.Start > DateTimeOffset.UtcNow.AddDays(90))
            return BadRequest(new { message = "Choose an available time between 10:00 and 20:00 Romania time, within the next 90 days." });
        try
        {
            await calendar.CreateBookingAsync(name, email, request.Start, cancellationToken);
            try { await emailService.SendBookingNotificationAsync(name, email, request.Start, cancellationToken); }
            catch (Exception ex) { logger.LogWarning(ex, "Booking succeeded but the psychologist notification email could not be sent."); }
            return Ok(new { message = "Session booked." });
        }
        catch (CalendarSlotUnavailableException)
        {
            return Conflict(new { message = "This time was just booked. Please choose another time." });
        }
        catch (CalendarNotConfiguredException)
        {
            return StatusCode(503, new { message = "Online booking is being set up. Please contact the practice directly." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not create calendar booking.");
            return StatusCode(502, new { message = "The booking could not be completed. Please try again." });
        }
    }

    public sealed class BookingRequest
    {
        [Required] public DateTimeOffset Start { get; set; }
    }
}

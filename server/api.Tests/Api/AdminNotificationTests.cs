using System.Net;
using System.Net.Http.Json;
using api.Configuration;
using api.Models;
using api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace api.Tests.Api;

/// <summary>How the admin finds out about new requests: the bell count endpoint and the email recipients.</summary>
public sealed class AdminNotificationTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private sealed record CountResponse(int Count);

    [Fact]
    public async Task Pending_count_requires_login()
    {
        var response = await _api.Anonymous().GetAsync("/api/admin/bookings/pending-count");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Pending_count_is_admin_only()
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.GetAsync("/api/admin/bookings/pending-count");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Pending_count_returns_the_number_of_waiting_requests()
    {
        var client = await _api.AddUserAsync();
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 0));
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 1));
        await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 2), BookingStatus.Approved);
        var (_, admin) = await _api.NewAdminAsync();

        var body = await admin.GetFromJsonAsync<CountResponse>("/api/admin/bookings/pending-count", ApiFactory.Json);

        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task Bell_follows_the_whole_flow()
    {
        var (_, client) = await _api.NewClientAsync();
        var (_, admin) = await _api.NewAdminAsync();
        async Task<int> Bell() => (await admin.GetFromJsonAsync<CountResponse>("/api/admin/bookings/pending-count", ApiFactory.Json))!.Count;

        Assert.Equal(0, await Bell());

        var response = await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(_day, 4) });
        var id = (await ApiFactory.ReadAsync<api.DTOs.Bookings.MyBookingDto>(response))!.Id;
        Assert.Equal(1, await Bell());

        await admin.PostAsync($"/api/admin/bookings/{id}/approve", null);
        Assert.Equal(0, await Bell());
    }

    [Fact]
    public void New_request_emails_are_configured_to_reach_the_practice_gmail()
    {
        // Only the Resend account owner's address while ancaberbunschi.ro is not verified in Resend:
        // with the test sender (onboarding@resend.dev) any other recipient makes Resend refuse the email.
        var settings = _api.Services.GetRequiredService<IOptions<EmailSettings>>().Value;

        Assert.Equal(["psihologancaalexandra@gmail.com"], settings.BookingRequestTo);
    }

    public void Dispose() => _api.Dispose();
}

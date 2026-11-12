using System.Net;
using System.Net.Http.Json;
using api.DTOs.Admin;
using api.DTOs.Bookings;
using api.DTOs.Common;
using api.Models;
using api.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Api;

/// <summary>Admin "Users" tab: list, search, paging, session counts, a user's sessions, role changes, deletion.</summary>
public sealed class AdminUsersEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    private Task<PagedResult<AdminUserDto>?> ListAsync(HttpClient admin, string query = "") =>
        admin.GetFromJsonAsync<PagedResult<AdminUserDto>>($"/api/admin/users{query}", ApiFactory.Json);

    // ---- Listing ----------------------------------------------------------------------

    [Fact]
    public async Task Admin_sees_every_user_with_role_and_dates()
    {
        await _api.AddUserAsync("Ioana Ionescu", "ioana@example.com");
        await _api.AddUserAsync("Mihai Pop", "mihai@example.com");
        var (_, admin) = await _api.NewAdminAsync();

        var page = await ListAsync(admin);

        Assert.Equal(3, page!.TotalCount);
        var ioana = page.Items.Single(u => u.Email == "ioana@example.com");
        Assert.Equal("Ioana Ionescu", ioana.FullName);
        Assert.Equal("User", ioana.Role);
        Assert.Contains(page.Items, u => u.Role == "Admin");
    }

    [Fact]
    public async Task List_never_exposes_password_hashes()
    {
        await _api.AddUserAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var json = await (await admin.GetAsync("/api/admin/users")).Content.ReadAsStringAsync();

        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AQAAAA", json); // prefix of ASP.NET password hashes
    }

    [Fact]
    public async Task List_shows_each_clients_session_counts()
    {
        var ioana = await _api.AddUserAsync("Ioana", "ioana@example.com");
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 0), BookingStatus.Approved);
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 1), BookingStatus.Approved);
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 2), BookingStatus.Pending);
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 3), BookingStatus.Rejected);
        await _api.AddUserAsync("Nobody", "nobody@example.com");
        var (_, admin) = await _api.NewAdminAsync();

        var page = await ListAsync(admin);

        var row = page!.Items.Single(u => u.Email == "ioana@example.com");
        Assert.Equal(2, row.ApprovedBookings);
        Assert.Equal(1, row.PendingBookings);
        var empty = page.Items.Single(u => u.Email == "nobody@example.com");
        Assert.Equal(0, empty.ApprovedBookings);
        Assert.Equal(0, empty.PendingBookings);
    }

    [Theory]
    [InlineData("ioana", 1)]
    [InlineData("IONESCU", 1)]
    [InlineData("@clinic.test", 2)]
    [InlineData("nobody-matches", 0)]
    public async Task Search_matches_name_or_email_case_insensitively(string search, int expected)
    {
        await _api.AddUserAsync("Ioana Ionescu", "ioana@clinic.test");
        await _api.AddUserAsync("Mihai Pop", "mihai@clinic.test");
        var (_, admin) = await _api.NewAdminAsync();

        var page = await ListAsync(admin, $"?search={Uri.EscapeDataString(search)}");

        Assert.Equal(expected, page!.TotalCount);
    }

    [Fact]
    public async Task List_is_paged_newest_first()
    {
        for (var i = 0; i < 5; i++) await _api.AddUserAsync($"User {i}");
        var (_, admin) = await _api.NewAdminAsync();

        var first = await ListAsync(admin, "?page=1&pageSize=2");
        var third = await ListAsync(admin, "?page=3&pageSize=2");

        Assert.Equal(6, first!.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal("Anca Admin", first.Items[0].FullName); // created last
        Assert.Equal(2, third!.Items.Count);
        Assert.Empty(first.Items.Select(u => u.Id).Intersect(third.Items.Select(u => u.Id)));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?pageSize=0")]
    [InlineData("?pageSize=101")]
    public async Task Invalid_paging_is_a_bad_request(string query)
    {
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.GetAsync($"/api/admin/users{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- One client's sessions ------------------------------------------------------

    [Fact]
    public async Task Admin_can_list_the_sessions_of_one_client()
    {
        var ioana = await _api.AddUserAsync("Ioana");
        var mihai = await _api.AddUserAsync("Mihai");
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 0), BookingStatus.Approved);
        await _api.AddBookingAsync(ioana.Id, TestTime.Slot(_day, 1));
        await _api.AddBookingAsync(mihai.Id, TestTime.Slot(_day, 2));
        var (_, admin) = await _api.NewAdminAsync();

        var sessions = await admin.GetFromJsonAsync<List<AdminBookingDto>>($"/api/admin/bookings?userId={ioana.Id}", ApiFactory.Json);

        Assert.Equal(2, sessions!.Count);
        Assert.All(sessions, s => Assert.Equal("Ioana", s.ClientName));
    }

    // ---- Read-only: roles and deletions are handled by the developers ----------------

    [Fact]
    public async Task Admin_cannot_change_a_users_role_from_the_api()
    {
        var client = await _api.AddUserAsync();
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.PutAsJsonAsync($"/api/admin/users/{client.Id}/role", new { role = "Admin" });

        Assert.False(response.IsSuccessStatusCode, $"Expected failure, got {(int)response.StatusCode}");
        await using var db = _api.CreateDbContext();
        Assert.Equal(UserRole.User, (await db.Users.SingleAsync(u => u.Id == client.Id)).RoleId);
    }

    [Fact]
    public async Task Admin_cannot_delete_users_from_the_api()
    {
        var client = await _api.AddUserAsync();
        var booking = await _api.AddBookingAsync(client.Id, TestTime.Slot(_day, 0));
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.DeleteAsync($"/api/admin/users/{client.Id}");

        Assert.False(response.IsSuccessStatusCode, $"Expected failure, got {(int)response.StatusCode}");
        Assert.NotNull(await _api.FindBookingAsync(booking.Id));
        Assert.Equal(2, (await ListAsync(admin))!.TotalCount);
    }

    [Fact]
    public async Task Admin_can_open_a_single_user()
    {
        var client = await _api.AddUserAsync("Ioana", "ioana@clinic.test");
        var (_, admin) = await _api.NewAdminAsync();

        var user = await admin.GetFromJsonAsync<api.DTOs.Auth.UserDto>($"/api/admin/users/{client.Id}", ApiFactory.Json);

        Assert.Equal("ioana@clinic.test", user!.Email);
    }

    [Fact]
    public async Task Unknown_single_user_returns_404()
    {
        var (_, admin) = await _api.NewAdminAsync();

        var response = await admin.GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _api.Dispose();
}

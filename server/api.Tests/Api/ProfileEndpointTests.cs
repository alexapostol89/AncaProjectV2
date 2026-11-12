using System.Net;
using System.Net.Http.Json;
using api.DTOs.Auth;
using api.Models;
using api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Api;

/// <summary>"My profile": view, rename, change password, delete account. Works for clients and the admin.</summary>
public sealed class ProfileEndpointTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private const string Password = "Parola123!"; // what SqliteDb.AddUserAsync sets

    private async Task<User> ReloadAsync(Guid id)
    {
        await using var db = _api.CreateDbContext();
        return await db.Users.AsNoTracking().SingleAsync(u => u.Id == id);
    }

    // ---- View ---------------------------------------------------------------------

    [Fact]
    public async Task Me_returns_the_logged_in_users_profile_without_secrets()
    {
        var (user, client) = await _api.NewClientAsync("Ioana Ionescu");

        var response = await client.GetAsync("/api/auth/me");
        var json = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await ApiFactory.ReadAsync<UserDto>(response);
        Assert.Equal(user.Id, me!.Id);
        Assert.Equal("Ioana Ionescu", me.FullName);
        Assert.Equal("User", me.Role);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("resetToken", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_open_their_profile_too()
    {
        var (_, admin) = await _api.NewAdminAsync();

        var me = await admin.GetFromJsonAsync<UserDto>("/api/auth/me", ApiFactory.Json);

        Assert.Equal("Admin", me!.Role);
    }

    // ---- Rename -------------------------------------------------------------------

    [Fact]
    public async Task User_can_change_their_name()
    {
        var (user, client) = await _api.NewClientAsync("Old Name");

        var response = await client.PutAsJsonAsync("/api/auth/me", new { fullName = "  Ioana Maria Ionescu  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ioana Maria Ionescu", (await ApiFactory.ReadAsync<UserDto>(response))!.FullName);
        Assert.Equal("Ioana Maria Ionescu", (await ReloadAsync(user.Id)).FullName);
    }

    [Fact]
    public async Task Renaming_keeps_the_same_token_working()
    {
        var (_, client) = await _api.NewClientAsync();

        await client.PutAsJsonAsync("/api/auth/me", new { fullName = "New Name" });

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    public async Task Too_short_name_is_rejected(string name)
    {
        var (user, client) = await _api.NewClientAsync("Original");

        var response = await client.PutAsJsonAsync("/api/auth/me", new { fullName = name });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Original", (await ReloadAsync(user.Id)).FullName);
    }

    [Fact]
    public async Task Too_long_name_is_rejected()
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PutAsJsonAsync("/api/auth/me", new { fullName = new string('a', 121) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profile_update_cannot_change_email_or_role()
    {
        var (user, client) = await _api.NewClientAsync("Ioana");

        await client.PutAsJsonAsync("/api/auth/me", new { fullName = "Ioana", email = "hacker@example.com", role = "Admin" });

        var saved = await ReloadAsync(user.Id);
        Assert.Equal(user.Email, saved.Email);
        Assert.Equal(UserRole.User, saved.RoleId);
    }

    [Fact]
    public async Task New_name_appears_on_the_next_booking_request_email()
    {
        var (_, client) = await _api.NewClientAsync("Old Name");
        await client.PutAsJsonAsync("/api/auth/me", new { fullName = "New Name" });

        await client.PostAsJsonAsync("/api/bookings", new { start = TestTime.Slot(TestTime.FutureWorkday(), 0) });

        Assert.Equal("New Name", Assert.Single(_api.Email.OfKind("Requested")).Name);
    }

    // ---- Change password ------------------------------------------------------------

    [Fact]
    public async Task User_can_change_their_password_with_the_current_one()
    {
        var (user, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = Password, newPassword = "NouaParola9" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await ReloadAsync(user.Id);
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, "NouaParola9"));
    }

    [Fact]
    public async Task Wrong_current_password_is_refused()
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Wrong123!", newPassword = "NouaParola9" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("wrong_password", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public async Task Weak_new_password_is_refused(string weak)
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = Password, newPassword = weak });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Delete account ----------------------------------------------------------------

    [Fact]
    public async Task User_can_delete_their_account_and_their_bookings_go_too()
    {
        var (user, client) = await _api.NewClientAsync();
        var booking = await _api.AddBookingAsync(user.Id, TestTime.Slot(TestTime.FutureWorkday(), 0));

        var response = await client.PostAsJsonAsync("/api/auth/delete-account", new { password = Password });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await _api.FindBookingAsync(booking.Id));
        await using var db = _api.CreateDbContext();
        Assert.False(await db.Users.AnyAsync(u => u.Id == user.Id));
    }

    [Fact]
    public async Task Deleting_requires_the_correct_password()
    {
        var (user, client) = await _api.NewClientAsync();

        var response = await client.PostAsJsonAsync("/api/auth/delete-account", new { password = "Wrong123!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await ReloadAsync(user.Id));
    }

    [Fact]
    public async Task Token_stops_working_after_deleting_the_account()
    {
        var (_, client) = await _api.NewClientAsync();
        await client.PostAsJsonAsync("/api/auth/delete-account", new { password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_delete_their_account_from_the_website()
    {
        var (admin, adminClient) = await _api.NewAdminAsync();

        var response = await adminClient.PostAsJsonAsync("/api/auth/delete-account", new { password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("admin_account", await response.Content.ReadAsStringAsync());
        Assert.NotNull(await ReloadAsync(admin.Id));
    }

    [Fact]
    public async Task Admin_cannot_delete_their_account_even_when_other_admins_exist()
    {
        await _api.NewAdminAsync();
        var (admin, adminClient) = await _api.NewAdminAsync();

        var response = await adminClient.PostAsJsonAsync("/api/auth/delete-account", new { password = Password });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(await ReloadAsync(admin.Id));
    }

    public void Dispose() => _api.Dispose();
}

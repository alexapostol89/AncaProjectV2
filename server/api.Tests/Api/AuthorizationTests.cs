using System.Net;
using System.Net.Http.Json;
using api.Models;
using api.Tests.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests.Api;

/// <summary>
/// Who may call what. The first tests scan every endpoint in the app, so a new endpoint that is
/// accidentally public, or an admin endpoint without the Admin role, fails the build.
/// </summary>
public sealed class AuthorizationTests : IDisposable
{
    private readonly ApiFactory _api = new();
    private readonly DateOnly _day = TestTime.FutureWorkday();

    /// <summary>The only endpoints anyone may call without logging in.</summary>
    private static readonly HashSet<string> PublicEndpoints =
    [
        "GET /",
        "GET /health",
        "POST /api/auth/register",
        "POST /api/auth/login",
        "POST /api/auth/forgot-password",
        "POST /api/auth/reset-password",
        "POST /api/contact",
    ];

    private List<(string Name, Endpoint Endpoint)> AllEndpoints()
    {
        _api.EnsureDatabase();
        return _api.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["ANY"])
                .Select(m => ($"{m} /{e.RoutePattern.RawText!.TrimStart('/').ToLowerInvariant()}".Replace("/{id:guid}", "/{id}"), (Endpoint)e)))
            .ToList();
    }

    private static IEnumerable<string> Roles(Endpoint e) =>
        e.Metadata.GetOrderedMetadata<IAuthorizeData>()
            .Where(a => !string.IsNullOrEmpty(a.Roles))
            .SelectMany(a => a.Roles!.Split(','));

    // ---- Rules checked on every endpoint ----------------------------------------------

    [Fact]
    public void Only_the_known_public_endpoints_allow_anonymous_access()
    {
        var anonymous = AllEndpoints()
            .Where(x => x.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Select(x => x.Name)
            .ToHashSet();

        Assert.Equal(PublicEndpoints.Order(), anonymous.Order());
    }

    [Fact]
    public void Every_admin_endpoint_requires_the_admin_role()
    {
        var admin = AllEndpoints().Where(x => x.Name.Contains(" /api/admin")).ToList();

        Assert.NotEmpty(admin);
        Assert.All(admin, x => Assert.True(Roles(x.Endpoint).SequenceEqual([nameof(UserRole.Admin)]), $"{x.Name} must be Admin-only"));
    }

    [Fact]
    public void Every_client_booking_endpoint_requires_the_user_role()
    {
        var booking = AllEndpoints().Where(x => x.Name.Contains(" /api/bookings")).ToList();

        Assert.NotEmpty(booking);
        Assert.All(booking, x => Assert.True(Roles(x.Endpoint).SequenceEqual([nameof(UserRole.User)]), $"{x.Name} must be User-only"));
    }

    [Fact]
    public async Task Every_non_public_endpoint_answers_401_without_a_token()
    {
        var client = _api.Anonymous();
        var failures = new List<string>();

        foreach (var (name, _) in AllEndpoints().Where(x => !PublicEndpoints.Contains(x.Name)))
        {
            var parts = name.Split(' ', 2);
            var url = parts[1].Replace("{id}", Guid.NewGuid().ToString());
            var request = new HttpRequestMessage(new HttpMethod(parts[0]), url);
            if (parts[0] is "POST" or "PUT") request.Content = JsonContent.Create(new { });

            var response = await client.SendAsync(request);
            if (response.StatusCode != HttpStatusCode.Unauthorized) failures.Add($"{name} → {(int)response.StatusCode}");
        }

        Assert.Empty(failures);
    }

    // ---- Public endpoints really are public -------------------------------------------

    [Theory]
    [InlineData("/")]
    [InlineData("/health")]
    public async Task Public_get_endpoints_work_without_login(string url)
    {
        var response = await _api.Anonymous().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/forgot-password")]
    [InlineData("/api/auth/reset-password")]
    [InlineData("/api/contact")]
    public async Task Public_forms_are_reachable_without_login(string url)
    {
        // An empty body fails validation (400), which proves the request got past authorization.
        var response = await _api.Anonymous().PostAsJsonAsync(url, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---- Roles in practice -------------------------------------------------------------

    [Theory]
    [InlineData("GET", "/api/bookings/mine")]
    [InlineData("GET", "/api/bookings/availability?date=2030-01-07")]
    [InlineData("POST", "/api/bookings")]
    public async Task Admin_cannot_use_the_client_booking_endpoints(string method, string url)
    {
        var (_, admin) = await _api.NewAdminAsync();
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST") request.Content = JsonContent.Create(new { start = TestTime.Slot(_day, 0) });

        var response = await admin.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/admin/bookings")]
    [InlineData("/api/admin/bookings/pending-count")]
    [InlineData("/api/admin/users")]
    public async Task Client_cannot_read_admin_data(string url)
    {
        var (_, client) = await _api.NewClientAsync();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Client_cannot_promote_themselves_to_admin()
    {
        var (user, client) = await _api.NewClientAsync();

        var response = await client.PutAsJsonAsync($"/api/admin/users/{user.Id}/role", new { role = "Admin" });

        Assert.False(response.IsSuccessStatusCode);
        await using var db = _api.CreateDbContext();
        Assert.Equal(UserRole.User, (await db.Users.SingleAsync(u => u.Id == user.Id)).RoleId);
    }

    [Fact]
    public async Task Client_cannot_delete_other_users()
    {
        var victim = await _api.AddUserAsync("Victim");
        var (_, client) = await _api.NewClientAsync();

        var response = await client.DeleteAsync($"/api/admin/users/{victim.Id}");

        Assert.False(response.IsSuccessStatusCode);
        await using var db = _api.CreateDbContext();
        Assert.True(await db.Users.AnyAsync(u => u.Id == victim.Id));
    }

    [Fact]
    public void User_management_endpoints_are_read_only()
    {
        // Roles and deletions are done by the developers in the database, never through the API.
        var userEndpoints = AllEndpoints().Where(x => x.Name.Contains(" /api/admin/users")).Select(x => x.Name).ToList();

        Assert.NotEmpty(userEndpoints);
        Assert.All(userEndpoints, name => Assert.StartsWith("GET ", name));
    }

    [Fact]
    public void No_endpoint_can_change_roles()
    {
        var suspicious = AllEndpoints()
            .Where(x => !x.Name.StartsWith("GET ") && x.Name.Contains("role"))
            .Select(x => x.Name);

        Assert.Empty(suspicious);
    }

    // ---- Tokens follow the database, not just the signature ----------------------------

    private async Task SetRoleAsync(Guid userId, UserRole role)
    {
        await using var db = _api.CreateDbContext();
        (await db.Users.SingleAsync(u => u.Id == userId)).RoleId = role;
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Demoted_admin_loses_admin_access_immediately()
    {
        var (admin, adminClient) = await _api.NewAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await adminClient.GetAsync("/api/admin/bookings")).StatusCode);

        await SetRoleAsync(admin.Id, UserRole.User);

        Assert.Equal(HttpStatusCode.Unauthorized, (await adminClient.GetAsync("/api/admin/bookings")).StatusCode);
    }

    [Fact]
    public async Task Promoted_user_must_log_in_again_to_get_the_new_role()
    {
        var (user, client) = await _api.NewClientAsync();

        await SetRoleAsync(user.Id, UserRole.Admin);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Deleted_user_token_stops_working()
    {
        var (user, client) = await _api.NewClientAsync();
        await using (var db = _api.CreateDbContext())
        {
            db.Users.Remove(await db.Users.SingleAsync(u => u.Id == user.Id));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task Token_for_an_unknown_user_is_rejected()
    {
        var ghost = new User { Id = Guid.NewGuid(), Email = "ghost@example.com", FullName = "Ghost", RoleId = UserRole.Admin };

        var response = await _api.ClientFor(ghost).GetAsync("/api/admin/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unchanged_account_keeps_working()
    {
        var (_, client) = await _api.NewClientAsync();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/bookings/mine")).StatusCode);
    }

    public void Dispose() => _api.Dispose();
}

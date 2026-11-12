using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace api.Tests.Infrastructure;

/// <summary>
/// Runs the real API in memory (routing, JWT auth, role checks, validation, controllers, services)
/// with three swaps: SQLite instead of Neon, a fake Google Calendar and a fake email sender.
/// Create one per test (or per test class) to get a clean database.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly SqliteConnection _connection = new("DataSource=:memory:;Foreign Keys=True");
    private int _clientCount;

    public FakeCalendarService Calendar { get; } = new();
    public FakeEmailService Email { get; } = new();

    public ApiFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" skips appsettings.Development.json and user-secrets (no real DB or keys are touched).
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Host=not-used-in-tests");
        builder.UseSetting("Jwt:Key", "test-signing-key-that-is-definitely-longer-than-32-chars");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Seed:AdminEmail", "");
        builder.UseSetting("Seed:AdminPassword", "");
        builder.UseSetting("ClientApp:BaseUrl", "https://site.test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

            services.RemoveAll<IGoogleCalendarService>();
            services.AddSingleton<IGoogleCalendarService>(Calendar);
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        // The API rate-limits per client IP (10 booking calls / minute). Give every test client its
        // own IP via X-Forwarded-For (trusted by the app, as behind Fly's proxy) so tests don't share a budget.
        var n = Interlocked.Increment(ref _clientCount);
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{n / 250}.{n % 250 + 1}");
    }

    /// <summary>Starts the app (once) and creates the schema.</summary>
    public void EnsureDatabase()
    {
        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
    }

    public AppDbContext CreateDbContext()
    {
        EnsureDatabase();
        return Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    }

    public Task<User> AddUserAsync(string name = "Maria Popescu", string? email = null, UserRole role = UserRole.User) =>
        SqliteDb.AddUserAsync(CreateDbContext(), name, email, role);

    public Task<Booking> AddBookingAsync(Guid userId, DateTimeOffset start, BookingStatus status = BookingStatus.Pending) =>
        SqliteDb.AddBookingAsync(CreateDbContext(), userId, start, status);

    public async Task<Booking?> FindBookingAsync(Guid id)
    {
        await using var db = CreateDbContext();
        return await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>An HttpClient with no login.</summary>
    public HttpClient Anonymous()
    {
        EnsureDatabase();
        return CreateClient();
    }

    /// <summary>An HttpClient logged in as <paramref name="user"/> (a real JWT signed by the app).</summary>
    public HttpClient ClientFor(User user)
    {
        var client = Anonymous();
        var token = Services.GetRequiredService<ITokenService>().CreateToken(user).Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<(User User, HttpClient Client)> NewClientAsync(string name = "Maria Popescu")
    {
        var user = await AddUserAsync(name);
        return (user, ClientFor(user));
    }

    public async Task<(User User, HttpClient Client)> NewAdminAsync()
    {
        var admin = await AddUserAsync("Anca Admin", role: UserRole.Admin);
        return (admin, ClientFor(admin));
    }

    public static Task<T?> ReadAsync<T>(HttpResponseMessage response) => response.Content.ReadFromJsonAsync<T>(Json);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

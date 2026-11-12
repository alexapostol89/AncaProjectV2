using api.Configuration;
using api.Data;
using api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace api.Tests.Infrastructure;

/// <summary>
/// Base for BookingService tests: a fresh database, a fake Google Calendar and a fake email sender per test.
/// xUnit creates a new instance of the test class for every test, so nothing leaks between tests.
/// </summary>
public abstract class BookingServiceTestBase : IDisposable
{
    protected const string ClientBaseUrl = "https://site.test";

    protected SqliteDb Db { get; } = new();
    protected FakeCalendarService Calendar { get; } = new();
    protected FakeEmailService Email { get; } = new();

    /// <summary>A date a week ahead, Monday–Saturday, so every slot is in the future.</summary>
    protected DateOnly Day { get; } = TestTime.FutureWorkday();

    /// <summary>A new service with its own DbContext, like one HTTP request.</summary>
    protected BookingService CreateService(AppDbContext? context = null) => new(
        context ?? Db.CreateContext(),
        Calendar,
        Email,
        Options.Create(new ClientAppSettings { BaseUrl = ClientBaseUrl + "/" }),
        NullLogger<BookingService>.Instance);

    public void Dispose()
    {
        Db.Dispose();
        GC.SuppressFinalize(this);
    }
}

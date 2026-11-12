using api.Data;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.Infrastructure;

/// <summary>
/// A fresh in-memory SQLite database per test, built from the real AppDbContext model
/// (same tables, foreign keys and the filtered unique index on Bookings.StartUtc).
/// The open connection keeps the database alive until Dispose.
/// </summary>
public sealed class SqliteDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        _connection.Open();
        using var db = CreateContext();
        db.Database.EnsureCreated();
    }

    public DbContextOptions<AppDbContext> Options =>
        new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;

    /// <summary>A new context on the same database, like a new HTTP request would get.</summary>
    public AppDbContext CreateContext() => new(Options);

    public Task<User> AddUserAsync(string name = "Maria Popescu", string? email = null, UserRole role = UserRole.User) =>
        AddUserAsync(CreateContext(), name, email, role);

    public static async Task<User> AddUserAsync(AppDbContext db, string name = "Maria Popescu", string? email = null, UserRole role = UserRole.User)
    {
        await using (db)
        {
            var user = new User
            {
                FullName = name,
                Email = email ?? $"{Guid.NewGuid():N}@example.com",
                RoleId = role
            };
            user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "Parola123!");
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }
    }

    public Task<Booking> AddBookingAsync(Guid userId, DateTimeOffset start, BookingStatus status = BookingStatus.Pending, int minutes = 50) =>
        AddBookingAsync(CreateContext(), userId, start, status, minutes);

    public static async Task<Booking> AddBookingAsync(AppDbContext db, Guid userId, DateTimeOffset start, BookingStatus status = BookingStatus.Pending, int minutes = 50)
    {
        await using (db)
        {
            var booking = new Booking
            {
                UserId = userId,
                StartUtc = start.UtcDateTime,
                EndUtc = start.AddMinutes(minutes).UtcDateTime,
                Status = status
            };
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();
            return booking;
        }
    }

    public async Task<Booking?> FindBookingAsync(Guid id)
    {
        await using var db = CreateContext();
        return await db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
    }

    public void Dispose() => _connection.Dispose();
}

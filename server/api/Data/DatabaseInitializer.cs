using api.Configuration;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Data;

/// <summary>
/// Runs once at startup:
///   1. applies pending EF migrations — only if Database:MigrateOnStartup is true
///      (on in Development; in production run "dotnet ef database update" yourself),
///   2. creates the first admin from the Seed:* settings if that email doesn't exist yet.
/// Public registration only ever creates normal users, so this is how the first admin gets in.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var db = provider.GetRequiredService<AppDbContext>();
        var configuration = provider.GetRequiredService<IConfiguration>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseInitializer));

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await db.Database.MigrateAsync();
        }

        var seed = provider.GetRequiredService<IOptions<SeedSettings>>().Value;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.AdminPassword))
        {
            return;
        }

        var email = seed.AdminEmail.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            return;
        }

        var admin = new User
        {
            Email = email,
            FullName = seed.AdminFullName,
            RoleId = UserRole.Admin
        };
        admin.PasswordHash = provider.GetRequiredService<IPasswordHasher<User>>().HashPassword(admin, seed.AdminPassword);

        db.Users.Add(admin);
        await db.SaveChangesAsync();
        logger.LogInformation("Seed admin account created.");
    }
}

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using api.Configuration;
using api.Data;
using api.Models;
using api.Services;

// Local development:
//  - server/api/.env (copied from .env.example) → environment variables
//  - DATABASE_URL from the repo-root .env.local written by the Neon CLI (used if no connection string is set)
DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
LoadLocalDatabaseUrl();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ---- Configuration ---------------------------------------------------
// Bound from appsettings.json / user-secrets (dev) / environment variables (prod).
// Secrets (never in appsettings.json):
//   ConnectionStrings__Default, Jwt__Key, Email__ApiKey, Seed__AdminPassword
builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection(EmailSettings.SectionName))
    .ValidateOnStart();

builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .Validate(s => s.Key.Length >= 32, "Jwt:Key must be set and at least 32 characters long.")
    .Validate(s => s.ExpiryHours > 0, "Jwt:ExpiryHours must be positive.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ClientAppSettings>()
    .Bind(builder.Configuration.GetSection(ClientAppSettings.SectionName))
    .Validate(s => Uri.IsWellFormedUriString(s.BaseUrl, UriKind.Absolute), "ClientApp:BaseUrl must be an absolute URL.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SeedSettings>()
    .Bind(builder.Configuration.GetSection(SeedSettings.SectionName));

// ---- Database (PostgreSQL on Neon) -------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default");
var neonDatabaseUrl = builder.Configuration["DATABASE_URL"];
if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(neonDatabaseUrl))
{
    connectionString = ToNpgsqlConnectionString(neonDatabaseUrl);
    builder.Configuration["ConnectionStrings:Default"] = connectionString;
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:Default is not configured. Dev: dotnet user-secrets set \"ConnectionStrings:Default\" \"...\". " +
        "Prod: fly secrets set ConnectionStrings__Default=\"...\".");
}

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// ---- Authentication ------------------------------------------------------
// Hashes passwords (salted PBKDF2). Comes with ASP.NET; no Identity tables needed.
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

// Every request with "Authorization: Bearer <token>" is checked here: signature, issuer, audience, expiry.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtSettings>>((options, jwtOptions) =>
    {
        var jwt = jwtOptions.Value;
        options.MapInboundClaims = false; // keep the short claim names "sub", "role", "name"
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = AppClaimTypes.Name,
            RoleClaimType = AppClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ---- Services ----------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient<IEmailService, EmailService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

// Fly.io terminates TLS at its proxy and forwards the real client IP in
// X-Forwarded-For. Without this, every request looks like it comes from the
// proxy and the per-IP rate limits would be shared by all visitors.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1; // only trust the hop Fly's proxy added
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// CORS: only the configured frontend origin(s) may call this API.
// Set via appsettings ("Cors:AllowedOrigins") or env var Cors__AllowedOrigins__0 etc.
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

const string corsPolicyName = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE");
    });
});

// Rate limiting per client IP — see RateLimitPolicies for the limits.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Contact, httpContext => PerIp(httpContext, permitLimit: 5));
    options.AddPolicy(RateLimitPolicies.Auth, httpContext => PerIp(httpContext, permitLimit: 10));

    static RateLimitPartition<string> PerIp(HttpContext httpContext, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => "Anca Berbunschi — Psihologie API");
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var connected = await db.Database.CanConnectAsync(cancellationToken);
    return connected
        ? Results.Ok(new { status = "ok", database = "ok" })
        : Results.Problem("Database unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
});

// Migrations (dev only, see Database:MigrateOnStartup) + first admin.
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

static void LoadLocalDatabaseUrl()
{
    if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DATABASE_URL")))
    {
        return;
    }

    // Neon CLI writes local credentials at the repository root. Search upward
    // so `dotnet run` works from either the repo root or server/api.
    for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory is not null; directory = directory.Parent)
    {
        var envPath = Path.Combine(directory.FullName, ".env.local");
        if (!File.Exists(envPath))
        {
            continue;
        }

        foreach (var line in File.ReadLines(envPath))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("DATABASE_URL=", StringComparison.Ordinal))
            {
                var value = trimmedLine["DATABASE_URL=".Length..].Trim();
                if (value.Length >= 2 && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value[1..^1];
                }

                Environment.SetEnvironmentVariable("DATABASE_URL", value);
                return;
            }
        }

        return;
    }
}

static string ToNpgsqlConnectionString(string databaseUrl)
{
    if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
    {
        throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL URL.");
    }

    var credentialParts = uri.UserInfo.Split(':', 2);
    if (credentialParts.Length != 2)
    {
        throw new InvalidOperationException("DATABASE_URL must include PostgreSQL credentials.");
    }

    return new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(credentialParts[0]),
        Password = Uri.UnescapeDataString(credentialParts[1]),
        SslMode = SslMode.Require,
        Pooling = true,
        MaxPoolSize = 10
    }.ConnectionString;
}

using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using api.Configuration;
using api.Data;
using api.Models;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

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
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Migrations (dev only, see Database:MigrateOnStartup) + first admin.
await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

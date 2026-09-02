using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using api.Configuration;
using api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration ---------------------------------------------------
// Bound from appsettings.json (dev) / environment variables (prod):
//   Email__ApiKey, Email__From, Email__FromName, Email__To
builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection(EmailSettings.SectionName))
    .ValidateOnStart();

// ---- Services ----------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpClient<IEmailService, EmailService>();

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
              .WithMethods("GET", "POST");
    });
});

// Basic rate limiting on the contact endpoint to slow down spam/abuse:
// 5 requests per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("contact", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(corsPolicyName);
app.UseRateLimiter();

app.MapControllers();

app.MapGet("/", () => "Anca Berbunschi — Psihologie API");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

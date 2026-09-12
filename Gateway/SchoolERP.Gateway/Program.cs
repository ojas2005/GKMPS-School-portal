using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SchoolERP.Shared.ExceptionHandling;
using SchoolERP.Shared.Hosting;
using SchoolERP.Shared.Logging;
using SchoolERP.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SharedLogging.Configure("Gateway"));

// ---------- YARP: routes/clusters loaded from appsettings ("ReverseProxy" section) ----------
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// ---------- CORS: the single place a browser-based frontend (Angular) needs to be
// allowed through, since all client traffic should go through this gateway rather than
// hitting individual services directly. Allowed origins come from appsettings
// ("Cors:AllowedOrigins") so prod/staging/dev can each list their own frontend URL(s)
// without a code change. Credentials aren't needed since auth uses a Bearer token in
// the Authorization header, not cookies. ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:4200" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ---------- JWT validation at the edge (same signing key/issuer/audience as Identity.API) ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"]!;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---------- Redis-backed rate limiting so limits hold across gateway instances ----------
// (Distributed cache registered for future token-bucket/Redis-script based limiter; the
//  limiter below uses the in-process partitioned limiter per gateway pod as a baseline,
//  matching the stricter-on-auth / standard-elsewhere policy required at this layer.)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "gateway:";
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 60/min: brute-force protection while still letting the owner batch-create
    // accounts (each admission/onboarding is a register call on this policy).
    options.AddPolicy("auth-strict", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: SharedHosting.ClientPartitionKey(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.AddPolicy("standard", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: SharedHosting.ClientPartitionKey(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 300,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

builder.Services.AddHealthChecks();

builder.Services.AddSharedExceptionHandling();

builder.Services.AddSharedForwardedHeaders();

var app = builder.Build();

// First in the pipeline: every later middleware (rate limiter, request logging) should see
// the real client IP rather than the proxy hop in front of this service.
app.UseForwardedHeaders();

app.UseExceptionHandler();

app.UseSecurityHeaders();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("Frontend");
// Authentication runs first so the rate limiter can bucket by signed-in user (see
// SharedHosting.ClientPartitionKey) instead of by the shared proxy IP.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");

// Auth-sensitive routes (login/register/refresh/OTP-style) get the strict limiter;
// everything else proxied through the gateway gets the standard one. YARP matches this
// via the RateLimiterPolicy set per-route in appsettings ReverseProxy:Routes.
app.MapReverseProxy();

app.Run();

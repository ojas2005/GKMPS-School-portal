using SchoolERP.Common;
using SchoolERP.Business.Identity.Sessions;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SchoolERP.Api.Startup;
using SchoolERP.Business;
using SchoolERP.Common.ExceptionHandling;
using SchoolERP.Common.Hosting;
using SchoolERP.Common.Logging;
using SchoolERP.Common.Security;
using SchoolERP.DataAccess.Identity;

// GKMPS School ERP -- single deployable, n-tier:
//   SchoolERP.Api (presentation) -> SchoolERP.Business (services) -> SchoolERP.DataAccess (EF Core,
//   repositories, storage), with SchoolERP.Common shared by all tiers.

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SharedLogging.Configure("SchoolERP.Api"));

// ---------- Business + data access tiers ----------
builder.Services.AddBusiness(builder.Configuration);
builder.Services.AddScoped<SchoolERP.Api.Security.StudentAccessGuard>();

// ---------- Authentication: JWT bearer ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32 || signingKey.StartsWith("CHANGE_ME"))
    throw new InvalidOperationException("Jwt:SigningKey must be set to a random value of at least 32 characters.");

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

        // A valid signature isn't enough: the token's sign-in session must still be open. This
        // is what makes sign-out, the inactivity timeout and admin sign-outs take effect at
        // once rather than when the 15-minute token runs out -- and it records the request as
        // activity, keeping the session alive while the user is using the app.
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var sessionId = context.Principal?.SessionId();
                if (sessionId is null)
                    return; // issued before sessions existed; it expires within minutes

                var userId = context.Principal!.UserId();
                var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
                if (userId is null || !await sessions.CheckAndTouchAsync(sessionId.Value, userId.Value, context.HttpContext.RequestAborted))
                    context.Fail("The session has ended.");
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS: only the frontend origin(s) may call the API from a browser ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---------- Rate limiting: per signed-in user, or per client IP when anonymous ----------
builder.Services.AddSharedForwardedHeaders();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Login/refresh/register: brute-force protection while still letting the owner batch-create accounts.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: SharedHosting.ClientPartitionKey(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // PDFs cost far more than ordinary requests (generation, storage, database units), so
    // downloading them has its own, much tighter allowance.
    options.AddPolicy("documents", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: SharedHosting.ClientPartitionKey(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

    // Everything else: a per-minute limit for bursts, and a daily cap. The database runs on a
    // free monthly allowance that stops the whole site when exhausted, so no single account
    // (even a legitimate one that's been compromised) may consume more than a sliver of it.
    // A busy staff member -- marking a whole school's attendance and browsing all day -- stays
    // well under both.
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: SharedHosting.ClientPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 180, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })),
        PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: SharedHosting.ClientPartitionKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5_000, Window = TimeSpan.FromDays(1), QueueLimit = 0 })));
});

// ---------- Controllers + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "GKMPS School ERP API", Version = "v1" });
    // Several modules have DTOs with the same short name; use full names as schema ids.
    c.CustomSchemaIds(type => type.FullName!.Replace('+', '.'));
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
});

// ---------- Health checks ----------
// Every module's database lives on the same server, so one check covers reachability.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("database", tags: new[] { "ready" });

builder.Services.AddSharedExceptionHandling();

var app = builder.Build();

// First in the pipeline so everything after it sees the real client IP behind the proxy.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSecurityHeaders();

if (app.IsSwaggerEnabled())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "GKMPS School ERP API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseCors("Frontend");
// Authentication runs before the rate limiter so requests are bucketed per signed-in user.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

DatabaseInitializer.Initialize(app);

app.Run();

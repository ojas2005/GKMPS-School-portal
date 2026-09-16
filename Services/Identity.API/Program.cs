using System.Text;
using System.Threading.RateLimiting;
using Asp.Versioning;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SchoolERP.Shared.ExceptionHandling;
using SchoolERP.Shared.Hosting;
using SchoolERP.Shared.Logging;
using SchoolERP.Identity.Auth;
using SchoolERP.Identity.Data;
using SchoolERP.Identity.Repositories;
using SchoolERP.Identity.Repositories.Interfaces;
using SchoolERP.Identity.Services;
using SchoolERP.Identity.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ---------- Logging (Serilog -> console + Seq) ----------
builder.Host.UseSerilog(SharedLogging.Configure("Identity.API"));

// ---------- EF Core / TiDB (MySQL wire protocol) ----------
var identityDbConnectionString = builder.Configuration.GetConnectionString("IdentityDb");
var tidbServerVersion = new MySqlServerVersion(new Version(8, 0, 11));
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseMySql(identityDbConnectionString, tidbServerVersion));

// ---------- Redis distributed cache ----------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "identity:";
});

// ---------- JWT options + generator ----------
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<ITokenGenerator, TokenGenerator>();

// ---------- Repositories (5-layer pattern: Entity -> Repository -> Service -> Controller) ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ---------- Services ----------
builder.Services.AddScoped<StudentProfileResolver>();
builder.Services.AddScoped<StaffProfileResolver>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

// ---------- MassTransit + RabbitMQ (Outbox pattern for reliable event publishing) ----------
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<IdentityDbContext>(o =>
    {
        o.UseMySql();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "rabbitmq", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// ---------- AuthN: JWT Bearer ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
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

// ---------- Rate limiting: stricter policy on auth endpoints, standard elsewhere ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 60/min: behind the gateway every caller shares the gateway's IP, and the owner's
    // batch account creation (admissions/onboarding) goes through this policy too.
    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: SharedHosting.ClientPartitionKey(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: SharedHosting.ClientPartitionKey(httpContext),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// ---------- Controllers, Swagger, API versioning ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SchoolERP Identity.API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// ---------- Health checks (/health, /health/ready, /health/live) ----------
builder.Services.AddHealthChecks()
    .AddMySql(identityDbConnectionString!, name: "mysql", tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });

builder.Services.AddSharedExceptionHandling();

builder.Services.AddSharedForwardedHeaders();

var app = builder.Build();

// First in the pipeline: every later middleware (rate limiter, request logging) should see
// the real client IP rather than the proxy hop in front of this service.
app.UseForwardedHeaders();

app.UseExceptionHandler();

// Swagger is off in Production unless Swagger__Enabled=true (see SharedHosting.IsSwaggerEnabled);
// the gateway's /{service}/swagger routes return 404 while it is off.
if (app.IsSwaggerEnabled())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
// Authentication runs first so the rate limiter can bucket by signed-in user (see
// SharedHosting.ClientPartitionKey) instead of by the shared proxy IP.
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Apply pending EF Core migrations automatically on startup (dev/staging convenience;
// production rollout uses the CI/CD pipeline's explicit migration step instead).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    SharedHosting.MigrateWithRetry(() => db.Database.Migrate(), app.Logger);

    // Seed the school-owner account: registration is closed to the public, so this is
    // the bootstrap identity every other account is created from. Credentials come from
    // Owner:Username / Owner:Password configuration; when no password is configured, a
    // random one is generated and logged once (retrieve it from the container logs and
    // change it immediately) -- never a hard-coded default, which would live forever in
    // git history for anyone to read.
    var ownerUsername = app.Configuration["Owner:Username"] ?? "ownerishim";
    if (!db.Users.IgnoreQueryFilters().Any(u => u.Username == ownerUsername))
    {
        var ownerPassword = app.Configuration["Owner:Password"];
        var passwordWasGenerated = string.IsNullOrEmpty(ownerPassword);
        if (passwordWasGenerated)
        {
            ownerPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18));
        }

        var owner = new SchoolERP.Identity.Entities.User
        {
            Email = $"{ownerUsername}@gkmps.local",
            Username = ownerUsername,
            FullName = "School Owner",
            Role = SchoolERP.Shared.Common.RoleNames.SuperAdmin,
            IsEmailVerified = true,
            IsActive = true
        };
        owner.PasswordHash = new Microsoft.AspNetCore.Identity.PasswordHasher<SchoolERP.Identity.Entities.User>()
            .HashPassword(owner, ownerPassword!);
        db.Users.Add(owner);
        db.SaveChanges();
        if (passwordWasGenerated)
        {
            Log.Warning("Seeded school-owner account '{Username}' with generated password '{Password}' -- log in and change it now, this is the only place it appears", ownerUsername, ownerPassword);
        }
        else
        {
            Log.Information("Seeded school-owner account '{Username}'", ownerUsername);
        }
    }
}

app.Run();

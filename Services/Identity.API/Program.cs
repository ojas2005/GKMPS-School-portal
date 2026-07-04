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

// ---------- EF Core / PostgreSQL ----------
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("IdentityDb")));

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
        o.UsePostgres();
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

// ---------- AuthN: JWT Bearer (primary) + Google (external login handshake) ----------
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
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
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
    .AddNpgSql(builder.Configuration.GetConnectionString("IdentityDb")!, name: "postgres", tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });

builder.Services.AddSharedExceptionHandling();

var app = builder.Build();

app.UseExceptionHandler();

// Swagger is left on in every environment (not gated to Development) so the gateway
// can proxy each service's docs for hands-on API testing. Before a real production
// rollout, gate this behind Development or an internal-only route/IP allowlist.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity.API v1");
    c.RoutePrefix = "swagger";
});

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
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
    db.Database.Migrate();

    // Seed the school-owner account: registration is closed to the public, so this is
    // the bootstrap identity every other account is created from. Credentials can be
    // overridden via Owner:Username / Owner:Password configuration.
    var ownerUsername = app.Configuration["Owner:Username"] ?? "ownerishim";
    var ownerPassword = app.Configuration["Owner:Password"] ?? "Owner@1234";
    if (!db.Users.IgnoreQueryFilters().Any(u => u.Username == ownerUsername))
    {
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
            .HashPassword(owner, ownerPassword);
        db.Users.Add(owner);
        db.SaveChanges();
        Log.Information("Seeded school-owner account '{Username}'", ownerUsername);
    }
}

app.Run();

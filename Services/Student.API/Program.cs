using System.Text;
using Asp.Versioning;
using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;
using SchoolERP.Shared.ExceptionHandling;
using SchoolERP.Shared.Hosting;
using SchoolERP.Shared.Logging;
using SchoolERP.Student.Data;
using SchoolERP.Student.Repositories;
using SchoolERP.Student.Repositories.Interfaces;
using SchoolERP.Student.Services;
using SchoolERP.Student.Services.Interfaces;
using SchoolERP.Student.Storage;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SharedLogging.Configure("Student.API"));

// ---------- EF Core / TiDB (MySQL wire protocol via Pomelo) ----------
var studentDbConnectionString = builder.Configuration.GetConnectionString("StudentDb");
var tidbServerVersion = new MySqlServerVersion(new Version(8, 0, 11));
builder.Services.AddDbContext<StudentDbContext>(options =>
    options.UseMySql(studentDbConnectionString, tidbServerVersion));

// ---------- Redis distributed cache ----------
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "student:";
});

// ---------- Azure Blob Storage ----------
builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

// ---------- Repositories ----------
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentDocumentRepository, StudentDocumentRepository>();
builder.Services.AddScoped<ITransferCertificateRepository, TransferCertificateRepository>();

// ---------- Services ----------
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ITransferCertificateService, TransferCertificateService>();

// ---------- MassTransit + RabbitMQ (Outbox pattern) ----------
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<StudentDbContext>(o =>
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

// ---------- AuthN: validates JWTs issued by Identity.API (shared signing key/issuer/audience) ----------
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

// ---------- Rate limiting: standard global policy (auth-specific limits live at the gateway/Identity.API) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: SharedHosting.ClientPartitionKey(httpContext),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SchoolERP Student.API", Version = "v1" });
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

builder.Services.AddHealthChecks()
    .AddMySql(studentDbConnectionString!, name: "mysql", tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });

builder.Services.AddSharedExceptionHandling();

builder.Services.AddSharedForwardedHeaders();

var app = builder.Build();

// First in the pipeline: every later middleware (rate limiter, request logging) should see
// the real client IP rather than the proxy hop in front of this service.
app.UseForwardedHeaders();

app.UseExceptionHandler();

// Swagger is left on in every environment so it can be used for hands-on API testing
// via the gateway or directly. Gate behind Development/an internal allowlist before a
// real production rollout.
if (app.IsSwaggerEnabled())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Student.API v1");
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StudentDbContext>();
    SharedHosting.MigrateWithRetry(() => db.Database.Migrate(), app.Logger);
}

app.Run();

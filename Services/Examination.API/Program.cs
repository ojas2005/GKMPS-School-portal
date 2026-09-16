using System.Text;
using Asp.Versioning;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Serilog;
using SchoolERP.Shared.ExceptionHandling;
using SchoolERP.Shared.Hosting;
using SchoolERP.Shared.Logging;
using SchoolERP.Examination.Data;
using SchoolERP.Examination.Repositories;
using SchoolERP.Examination.Repositories.Interfaces;
using SchoolERP.Examination.Services;
using SchoolERP.Examination.Services.Interfaces;
using SchoolERP.Examination.Storage;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog(SharedLogging.Configure("Examination.API"));

var examinationDbConnectionString = builder.Configuration.GetConnectionString("ExaminationDb");
var tidbServerVersion = new MySqlServerVersion(new Version(8, 0, 11));
builder.Services.AddDbContext<ExaminationDbContext>(options =>
    options.UseMySql(examinationDbConnectionString, tidbServerVersion));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "examination:";
});

builder.Services.AddSingleton(_ => new BlobServiceClient(builder.Configuration.GetConnectionString("BlobStorage")));
builder.Services.AddScoped<IBlobStorageService, AzureBlobStorageService>();

builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IMarksRepository, MarksRepository>();
builder.Services.AddScoped<IExamService, ExamService>();
builder.Services.AddScoped<IMarksService, MarksService>();

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

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: SharedHosting.ClientPartitionKey(httpContext),
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 300, // documented peak-load / HPA target alongside Attendance
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "SchoolERP Examination.API", Version = "v1" });
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
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddHealthChecks()
    .AddMySql(examinationDbConnectionString!, name: "mysql", tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });

builder.Services.AddSharedExceptionHandling();

builder.Services.AddSharedForwardedHeaders();

var app = builder.Build();

// First in the pipeline: every later middleware (rate limiter, request logging) should see
// the real client IP rather than the proxy hop in front of this service.
app.UseForwardedHeaders();

app.UseExceptionHandler();

if (app.IsSwaggerEnabled())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Examination.API v1");
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
    var db = scope.ServiceProvider.GetRequiredService<ExaminationDbContext>();
    SharedHosting.MigrateWithRetry(() => db.Database.Migrate(), app.Logger);
}

app.Run();

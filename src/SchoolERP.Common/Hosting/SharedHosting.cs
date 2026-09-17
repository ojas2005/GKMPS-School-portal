using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SchoolERP.Common.Hosting;

/// <summary>
/// Startup plumbing: real client IPs behind the proxy, rate-limit partitioning, Swagger
/// gating, and resilient startup migrations.
/// </summary>
public static class SharedHosting
{
    /// <summary>
    /// Traffic reaches the app as browser -> reverse proxy (Caddy, or the hosting platform's
    /// ingress) -> app, so Connection.RemoteIpAddress is the proxy. Honour X-Forwarded-For/Proto
    /// from that one hop (ForwardLimit = 1) so rate limiting and audit logs see the real
    /// client. Safe only while the app's port isn't reachable except through that proxy
    /// (docker-compose.yml binds it to loopback).
    /// </summary>
    public static IServiceCollection AddSharedForwardedHeaders(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

    /// <summary>
    /// Rate-limit bucket for a request: the signed-in user when there is one (so a whole
    /// school sharing one NAT/Wi-Fi IP doesn't share one bucket), otherwise the client IP.
    /// Requires UseAuthentication() to run before UseRateLimiter().
    /// </summary>
    public static string ClientPartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.User.FindFirst("sub")?.Value
            : null;
        return userId is not null
            ? $"user:{userId}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
    }

    /// <summary>
    /// Swagger exposes every endpoint and DTO, so it is off unless running in Development or
    /// explicitly enabled with Swagger__Enabled=true (e.g. a private staging box).
    /// </summary>
    public static bool IsSwaggerEnabled(this WebApplication app) =>
        app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled");

    /// <summary>
    /// Runs startup migrations, retrying while the database is still coming up (a cold
    /// TiDB cluster or container) instead of crashing the service into a restart loop.
    /// </summary>
    public static void MigrateWithRetry(Action migrate, ILogger logger, int maxAttempts = 10)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                migrate();
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 3 * attempt));
                logger.LogWarning(ex, "Database migration attempt {Attempt}/{MaxAttempts} failed; retrying in {Delay}", attempt, maxAttempts, delay);
                Thread.Sleep(delay);
            }
        }
    }
}

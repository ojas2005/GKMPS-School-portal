using Microsoft.AspNetCore.Routing;
using SchoolERP.Common;
using SchoolERP.Common.Audit;

namespace SchoolERP.Api.Security;

/// <summary>
/// Records every signed-in API request in the audit trail: who, which endpoint, which record,
/// the outcome and where from -- the "processing log" data-protection rules ask for. Sign-ins
/// and other anonymous security events are recorded explicitly by the services instead.
/// </summary>
public static class RequestAuditMiddleware
{
    public static IApplicationBuilder UseRequestAudit(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            await next();

            if (!context.Request.Path.StartsWithSegments("/api")
                || context.Request.Path.StartsWithSegments("/api/sessions/heartbeat") // keep-alive noise
                || context.User.Identity?.IsAuthenticated != true)
                return;

            // The route template, not the raw path, so entries group by endpoint; the record
            // it concerns (first id in the route) is kept separately for lookups.
            var template = (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? context.Request.Path.Value ?? "";
            var entityId = context.Request.RouteValues.Values
                .Select(v => v?.ToString())
                .FirstOrDefault(v => Guid.TryParse(v, out _));

            context.RequestServices.GetRequiredService<IAuditTrail>().Record(new AuditRecord(
                DateTime.UtcNow,
                $"{context.Request.Method} /{template.TrimStart('/')}",
                context.User.UserId(),
                context.User.Role(),
                entityId,
                context.Response.StatusCode,
                context.Connection.RemoteIpAddress?.ToString()));
        });
}

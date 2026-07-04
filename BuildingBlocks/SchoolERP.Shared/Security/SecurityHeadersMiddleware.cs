using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SchoolERP.Shared.Security;

/// <summary>
/// Adds the response headers a browser-facing JSON API should always send. Wired in at
/// the Gateway only -- per the CORS comment in Gateway/Program.cs, all browser traffic
/// goes through the gateway rather than hitting individual services directly, so this is
/// the one place these need to live. HSTS itself is set by Caddy (the Caddyfile), not
/// here, since Caddy is the actual TLS-terminating edge -- the gateway only ever sees
/// plain HTTP from Caddy internally, so it has no reliable way to know the original
/// request was HTTPS.
/// </summary>
public static class SecurityHeadersMiddleware
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            // Pure JSON API -- nothing here ever renders a script/style/frame, so the
            // strictest possible CSP is safe and doesn't need a script-src allowlist.
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            await next();
        });
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace SchoolERP.Common.Security;

/// <summary>
/// Adds the response headers a browser-facing JSON API should always send (wired in
/// Program.cs), including HSTS when the original request was HTTPS -- the cloud ingress
/// doesn't add it, so the app does (Caddy adds it too when self-hosted).
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
            // Browsers must use HTTPS for this host from now on. Behind Caddy or a cloud
            // ingress the request arrives as HTTP, so this relies on UseForwardedHeaders
            // having restored the original scheme.
            if (context.Request.IsHttps)
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

            await next();
        });
    }
}

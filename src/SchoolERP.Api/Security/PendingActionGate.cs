using SchoolERP.Common;

namespace SchoolERP.Api.Security;

/// <summary>
/// A token marked with a pending action ("change-password", "setup-two-factor") may reach
/// only what's needed to finish that step -- the rest of the API answers 403 with the action
/// in `errors`, so the browser can send the user to do it. Enforced here, not just in the
/// browser, so skipping the screen doesn't skip the step.
/// </summary>
public static class PendingActionGate
{
    // Exactly what finishing the step needs -- not all of /api/auth, which also creates accounts.
    private static readonly string[] AllowedPrefixes =
    [
        "/api/auth/change-password",
        "/api/auth/refresh",
        "/api/auth/logout",
        "/api/account/two-factor",
        "/api/sessions/heartbeat",
    ];

    public static IApplicationBuilder UsePendingActionGate(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var pending = context.User.PendingAction();
            var path = context.Request.Path.Value ?? "";
            if (pending is not null && !AllowedPrefixes.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) || path.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase)))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                    pending == "change-password"
                        ? "Please choose a new password before continuing."
                        : "Please set up two-step sign-in before continuing.",
                    [pending]));
                return;
            }
            await next();
        });
}

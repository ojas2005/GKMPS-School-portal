using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SchoolERP.Common;

namespace SchoolERP.Common.ExceptionHandling;

/// <summary>
/// Catches whatever a controller's own try/catch didn't anticipate. Registered via
/// <see cref="ServiceCollectionExtensions.AddSharedExceptionHandling"/> +
/// <c>app.UseExceptionHandler()</c> in Program.cs, so every endpoint fails the same way:
/// the real exception (with stack trace) goes to the log, the client only ever sees the
/// standard <see cref="ApiResponse{T}"/> envelope with a generic message -- never a raw
/// stack trace or an ASP.NET default problem-details shape the frontend doesn't expect.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // A malformed or oversized request is the client's problem, not a crash: answer with
        // its own status (400, 413...) and don't log it as a server error.
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(
                badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge ? "The request is too large." : "The request could not be read."),
                cancellationToken);
            return true;
        }

        _logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await httpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.Fail("Unexpected server error."),
            cancellationToken);

        return true;
    }
}

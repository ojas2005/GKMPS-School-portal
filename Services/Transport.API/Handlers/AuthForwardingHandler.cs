namespace SchoolERP.Transport.Handlers;

/// <summary>
/// Route detail needs student names, which Transport.API doesn't own, so it calls
/// Student.API's own authorized endpoint (never its database directly). This forwards
/// the caller's own Authorization header onto that outgoing call -- without it,
/// Student.API rejects every call with 401 and the typed client silently swallows that
/// into an empty/null result.
/// </summary>
public class AuthForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthForwardingHandler(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authHeader))
            request.Headers.TryAddWithoutValidation("Authorization", authHeader);

        return base.SendAsync(request, ct);
    }
}

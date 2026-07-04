namespace SchoolERP.Reporting.Handlers;

/// <summary>
/// Every report figure comes from another service's own authorized API (never its
/// database directly), so the outgoing call needs a token too. This forwards the
/// caller's own Authorization header onto every request made by the typed HttpClients
/// in Program.cs -- without it, Student.API/Fee.API reject every call with 401 and the
/// typed clients silently swallow that into an empty/zero result.
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

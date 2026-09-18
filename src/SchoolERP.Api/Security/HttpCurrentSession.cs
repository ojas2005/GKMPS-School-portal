using SchoolERP.Common;

namespace SchoolERP.Api.Security;

/// <summary>The session of the signed-in user making the current request.</summary>
public sealed class HttpCurrentSession(IHttpContextAccessor http) : ICurrentSession
{
    public Guid? SessionId => http.HttpContext?.User.SessionId();
}

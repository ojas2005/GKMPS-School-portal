namespace SchoolERP.Common;

/// <summary>The sign-in session of the request being handled, if any (see UserSession).</summary>
public interface ICurrentSession
{
    Guid? SessionId { get; }
}

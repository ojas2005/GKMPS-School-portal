using SchoolERP.DataAccess.Identity.Entities;

namespace SchoolERP.Business.Identity.Sessions;

public enum SessionState { Active, Idle, Expired, Ended }

/// <summary>The rules for whether a stored session may still be used -- kept pure so they're easy to test.</summary>
public static class SessionRules
{
    public static SessionState StateOf(DateTime? endedAtUtc, DateTime lastActivityUtc, DateTime expiresAtUtc, DateTime nowUtc, SessionOptions options)
    {
        if (endedAtUtc is not null) return SessionState.Ended;
        if (nowUtc >= expiresAtUtc) return SessionState.Expired;
        if (nowUtc - lastActivityUtc > options.IdleTimeout + SessionOptions.ServerGrace) return SessionState.Idle;
        return SessionState.Active;
    }

    public static SessionState StateOf(UserSession session, DateTime nowUtc, SessionOptions options) =>
        StateOf(session.EndedAtUtc, session.LastActivityUtc, session.ExpiresAtUtc, nowUtc, options);

    /// <summary>What to tell the user when a session can't continue.</summary>
    public static string Explain(SessionState state, SessionOptions options) => state switch
    {
        SessionState.Idle => $"You were signed out after {options.IdleTimeoutMinutes} minutes of inactivity. Please sign in again.",
        SessionState.Expired => "Your session has expired. Please sign in again.",
        _ => "You have been signed out. Please sign in again."
    };
}

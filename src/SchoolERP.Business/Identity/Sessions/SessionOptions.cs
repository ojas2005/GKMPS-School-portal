namespace SchoolERP.Business.Identity.Sessions;

public class SessionOptions
{
    public const string SectionName = "Session";

    /// <summary>
    /// Minutes without any activity after which the user is signed out. The browser signs
    /// out at exactly this point (after a one-minute warning); the server allows
    /// <see cref="ServerGrace"/> on top so its view of "last activity", which can lag the
    /// browser's by a few minutes, never ends a session the user is still using.
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 30;

    public TimeSpan IdleTimeout => TimeSpan.FromMinutes(IdleTimeoutMinutes);

    public static readonly TimeSpan ServerGrace = TimeSpan.FromMinutes(5);

    /// <summary>How long the server trusts a checked session before looking again (bounds DB reads per session).</summary>
    public static readonly TimeSpan CheckCacheFor = TimeSpan.FromSeconds(30);

    /// <summary>Last-activity is written at most this often per session (bounds DB writes per session).</summary>
    public static readonly TimeSpan TouchEvery = TimeSpan.FromMinutes(1);
}

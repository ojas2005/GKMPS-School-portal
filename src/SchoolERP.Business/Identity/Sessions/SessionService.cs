using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.Business.Identity.Sessions;

public interface ISessionService
{
    int IdleTimeoutMinutes { get; }

    Task<UserSession> StartAsync(Guid userId, DateTime expiresAtUtc, string? ipAddress, CancellationToken ct = default);

    /// <summary>
    /// For every authenticated request: is this session still usable? Records the request as
    /// activity. Answers from a short cache so a busy user doesn't cost a query per request.
    /// </summary>
    Task<bool> CheckAndTouchAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    /// <summary>For a token refresh: the session if it may continue (and marks it active), otherwise why not.</summary>
    Task<(UserSession? Session, SessionState State)> ContinueAsync(Guid sessionId, Guid userId, CancellationToken ct = default);

    Task EndAsync(Guid sessionId, string reason, CancellationToken ct = default);

    /// <summary>The message shown to the user when their session can't continue.</summary>
    string Explain(SessionState state);

    Task EndAllForUserAsync(Guid userId, string reason, CancellationToken ct = default);
}

public class SessionService : ISessionService
{
    private sealed record CachedSession(Guid UserId, DateTime LastActivityUtc, DateTime ExpiresAtUtc, DateTime? EndedAtUtc, long CachedAt);

    // Orders cache writes against "sign out everywhere": a session cached before that happened
    // must be re-read from the database. A counter, not a timestamp, so two things in the same
    // clock tick (ending all sessions, then starting the new one after a password change) still
    // come out in the right order.
    private static long _sequence;

    private readonly IUserSessionRepository _sessions;
    private readonly IMemoryCache _cache;
    private readonly SessionOptions _options;
    private readonly TimeProvider _clock;

    public SessionService(IUserSessionRepository sessions, IMemoryCache cache, IOptions<SessionOptions> options, TimeProvider clock)
    {
        _sessions = sessions;
        _cache = cache;
        _options = options.Value;
        _clock = clock;
    }

    public int IdleTimeoutMinutes => _options.IdleTimeoutMinutes;

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;
    private static string SessionKey(Guid id) => $"session:{id}";
    private static string UserEndedKey(Guid userId) => $"sessions-ended:{userId}";

    public async Task<UserSession> StartAsync(Guid userId, DateTime expiresAtUtc, string? ipAddress, CancellationToken ct = default)
    {
        var now = Now;
        var session = new UserSession { UserId = userId, CreatedAtUtc = now, LastActivityUtc = now, ExpiresAtUtc = expiresAtUtc, CreatedByIp = ipAddress };
        await _sessions.AddAsync(session, ct);
        await _sessions.SaveChangesAsync(ct);
        return session;
    }

    public async Task<bool> CheckAndTouchAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        var now = Now;
        if (!_cache.TryGetValue(SessionKey(sessionId), out CachedSession? cached) || cached is null)
        {
            cached = await LoadAsync(sessionId, ct);
            if (cached is null) return false;
        }
        else if (EndedForUserSince(cached))
        {
            cached = await LoadAsync(sessionId, ct);
            if (cached is null) return false;
        }

        if (cached.UserId != userId ||
            SessionRules.StateOf(cached.EndedAtUtc, cached.LastActivityUtc, cached.ExpiresAtUtc, now, _options) != SessionState.Active)
            return false;

        if (now - cached.LastActivityUtc >= SessionOptions.TouchEvery)
        {
            await _sessions.TouchAsync(sessionId, now, ct);
            _cache.Set(SessionKey(sessionId), cached with { LastActivityUtc = now }, SessionOptions.CheckCacheFor);
        }
        return true;
    }

    private async Task<CachedSession?> LoadAsync(Guid sessionId, CancellationToken ct)
    {
        var loadedAt = Interlocked.Increment(ref _sequence);
        var stored = await _sessions.FindByIdAsync(sessionId, ct);
        if (stored is null) return null;
        var cached = new CachedSession(stored.UserId, stored.LastActivityUtc, stored.ExpiresAtUtc, stored.EndedAtUtc, loadedAt);
        _cache.Set(SessionKey(sessionId), cached, SessionOptions.CheckCacheFor);
        return cached;
    }

    public async Task<(UserSession? Session, SessionState State)> ContinueAsync(Guid sessionId, Guid userId, CancellationToken ct = default)
    {
        var session = await _sessions.FindByIdAsync(sessionId, ct);
        if (session is null || session.UserId != userId)
            return (null, SessionState.Ended);

        var state = SessionRules.StateOf(session, Now, _options);
        if (state != SessionState.Active)
        {
            await EndAsync(sessionId, state == SessionState.Idle ? SessionEndReasons.Idle : SessionEndReasons.Expired, ct);
            return (null, state);
        }

        // A refresh only happens because the user did something, so it counts as activity.
        await _sessions.TouchAsync(sessionId, Now, ct);
        _cache.Remove(SessionKey(sessionId));
        return (session, state);
    }

    public async Task EndAsync(Guid sessionId, string reason, CancellationToken ct = default)
    {
        await _sessions.EndAsync(sessionId, reason, ct);
        _cache.Remove(SessionKey(sessionId));
    }

    public string Explain(SessionState state) => SessionRules.Explain(state, _options);

    public async Task EndAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
    {
        // End them in the database first, then mark the user: any of their sessions cached
        // before the mark is re-read (and now found ended) instead of trusted until its cache
        // entry lapses. Sessions started afterwards are read fresh and unaffected.
        await _sessions.EndAllForUserAsync(userId, reason, ct);
        _cache.Set(UserEndedKey(userId), Interlocked.Increment(ref _sequence), SessionOptions.CheckCacheFor + TimeSpan.FromSeconds(5));
    }

    private bool EndedForUserSince(CachedSession session) =>
        _cache.TryGetValue(UserEndedKey(session.UserId), out long endedAt) && session.CachedAt < endedAt;
}

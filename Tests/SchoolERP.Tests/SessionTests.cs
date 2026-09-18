using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SchoolERP.Business.Identity.Auth;
using SchoolERP.Business.Identity.Sessions;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.Tests;

public class SessionTests
{
    private sealed class FakeClock(DateTime start) : TimeProvider
    {
        public DateTime Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
        public void Advance(TimeSpan by) => Now += by;
    }

    // Stands in for the database: same semantics as the real UPDATE statements.
    private sealed class FakeSessions : IUserSessionRepository
    {
        public readonly Dictionary<Guid, UserSession> Rows = [];
        public int Touches;

        public Task<UserSession?> FindByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Rows.TryGetValue(id, out var s) ? Copy(s) : null);
        public Task AddAsync(UserSession session, CancellationToken ct = default) { Rows[session.Id] = session; return Task.CompletedTask; }
        public Task<int> TouchAsync(Guid id, DateTime at, CancellationToken ct = default)
        {
            Touches++;
            if (Rows.TryGetValue(id, out var s) && s.EndedAtUtc is null && s.LastActivityUtc < at) { s.LastActivityUtc = at; return Task.FromResult(1); }
            return Task.FromResult(0);
        }
        public Task<int> EndAsync(Guid id, string reason, CancellationToken ct = default)
        {
            if (Rows.TryGetValue(id, out var s) && s.EndedAtUtc is null) { s.EndedAtUtc = DateTime.UtcNow; s.EndReason = reason; return Task.FromResult(1); }
            return Task.FromResult(0);
        }
        public Task<int> EndAllForUserAsync(Guid userId, string reason, CancellationToken ct = default)
        {
            var n = 0;
            foreach (var s in Rows.Values.Where(s => s.UserId == userId && s.EndedAtUtc is null)) { s.EndedAtUtc = DateTime.UtcNow; s.EndReason = reason; n++; }
            return Task.FromResult(n);
        }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(0);
        private static UserSession Copy(UserSession s) => new()
        {
            Id = s.Id, UserId = s.UserId, CreatedAtUtc = s.CreatedAtUtc, LastActivityUtc = s.LastActivityUtc,
            ExpiresAtUtc = s.ExpiresAtUtc, EndedAtUtc = s.EndedAtUtc, EndReason = s.EndReason
        };
    }

    private static readonly DateTime Start = new(2026, 9, 19, 8, 0, 0, DateTimeKind.Utc);
    private static readonly Guid User = Guid.NewGuid();
    private static readonly SessionOptions ThirtyMinutes = new() { IdleTimeoutMinutes = 30 };

    private static (SessionService Service, FakeSessions Db, FakeClock Clock) Create()
    {
        var db = new FakeSessions();
        var clock = new FakeClock(Start);
        var service = new SessionService(db, new MemoryCache(new MemoryCacheOptions()), Options.Create(ThirtyMinutes), clock);
        return (service, db, clock);
    }

    // ---- the rules ----

    [Fact]
    public void A_session_used_recently_is_active()
    {
        Assert.Equal(SessionState.Active, SessionRules.StateOf(null, Start, Start.AddDays(7), Start.AddMinutes(29), ThirtyMinutes));
    }

    [Fact]
    public void The_server_allows_a_few_minutes_past_the_browser_timeout()
    {
        // The browser signs out at 30 minutes; the server's record of the last activity can lag,
        // so it waits a little longer rather than ending a session that's still in use.
        Assert.Equal(SessionState.Active, SessionRules.StateOf(null, Start, Start.AddDays(7), Start.AddMinutes(34), ThirtyMinutes));
        Assert.Equal(SessionState.Idle, SessionRules.StateOf(null, Start, Start.AddDays(7), Start.AddMinutes(36), ThirtyMinutes));
    }

    [Fact]
    public void Even_a_busy_session_ends_at_its_hard_cap()
    {
        var justUsed = Start.AddDays(7).AddMinutes(-1);
        Assert.Equal(SessionState.Expired, SessionRules.StateOf(null, justUsed, Start.AddDays(7), Start.AddDays(7), ThirtyMinutes));
    }

    [Fact]
    public void A_signed_out_session_stays_ended()
    {
        Assert.Equal(SessionState.Ended, SessionRules.StateOf(Start, Start, Start.AddDays(7), Start.AddMinutes(1), ThirtyMinutes));
    }

    // ---- the service over time ----

    [Fact]
    public async Task Staying_active_keeps_the_session_alive_for_hours()
    {
        var (sessions, _, clock) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);

        for (var i = 0; i < 18; i++)          // a request every 10 minutes for 3 hours
        {
            clock.Advance(TimeSpan.FromMinutes(10));
            Assert.True(await sessions.CheckAndTouchAsync(session.Id, User));
        }
    }

    [Fact]
    public async Task Going_idle_ends_the_session()
    {
        var (sessions, _, clock) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);

        clock.Advance(TimeSpan.FromMinutes(40));

        Assert.False(await sessions.CheckAndTouchAsync(session.Id, User));
    }

    [Fact]
    public async Task A_refresh_after_going_idle_is_refused_and_records_why()
    {
        var (sessions, db, clock) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);

        clock.Advance(TimeSpan.FromMinutes(40));
        var (continued, state) = await sessions.ContinueAsync(session.Id, User);

        Assert.Null(continued);
        Assert.Equal(SessionState.Idle, state);
        Assert.Equal(SessionEndReasons.Idle, db.Rows[session.Id].EndReason);
    }

    [Fact]
    public async Task Activity_is_written_at_most_once_a_minute()
    {
        var (sessions, db, clock) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);

        for (var i = 0; i < 30; i++)          // a request every 2 seconds for a minute
        {
            clock.Advance(TimeSpan.FromSeconds(2));
            await sessions.CheckAndTouchAsync(session.Id, User);
        }

        Assert.Equal(1, db.Touches);
    }

    [Fact]
    public async Task Signing_out_everywhere_takes_effect_immediately()
    {
        var (sessions, _, clock) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);
        Assert.True(await sessions.CheckAndTouchAsync(session.Id, User));   // now cached as valid

        await sessions.EndAllForUserAsync(User, SessionEndReasons.PasswordChanged);
        clock.Advance(TimeSpan.FromSeconds(1));
        var fresh = await sessions.StartAsync(User, Start.AddDays(7), null);

        Assert.False(await sessions.CheckAndTouchAsync(session.Id, User));
        Assert.True(await sessions.CheckAndTouchAsync(fresh.Id, User));
    }

    [Fact]
    public async Task A_session_cannot_be_used_with_another_users_token()
    {
        var (sessions, _, _) = Create();
        var session = await sessions.StartAsync(User, Start.AddDays(7), null);

        Assert.False(await sessions.CheckAndTouchAsync(session.Id, Guid.NewGuid()));
    }

    [Fact]
    public void Access_tokens_carry_their_session()
    {
        var generator = new TokenGenerator(Options.Create(new JwtOptions
        {
            Issuer = "test", Audience = "test", SigningKey = "0123456789abcdef0123456789abcdef"
        }));
        var sessionId = Guid.NewGuid();

        var token = new JwtSecurityTokenHandler().ReadJwtToken(
            generator.GenerateAccessToken(new User { Email = "a@b.c", FullName = "A", Role = "Teacher" }, sessionId: sessionId));

        Assert.Equal(sessionId.ToString(), token.Claims.Single(c => c.Type == "sid").Value);
    }
}

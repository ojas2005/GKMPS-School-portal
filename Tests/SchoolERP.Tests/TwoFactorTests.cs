using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using SchoolERP.Business.Identity.TwoFactor;
using SchoolERP.Common.Audit;
using SchoolERP.DataAccess.Identity.Entities;
using SchoolERP.DataAccess.Identity.Repositories.Interfaces;

namespace SchoolERP.Tests;

public class TwoFactorTests
{
    // ---- the code algorithm, against RFC 6238's published test vectors (SHA-1) ----

    [Theory]
    [InlineData(59, "94287082")]
    [InlineData(1111111109, "07081804")]
    [InlineData(1111111111, "14050471")]
    [InlineData(1234567890, "89005924")]
    [InlineData(2000000000, "69279037")]
    public void Codes_match_the_standards_test_vectors(long unixTime, string expected)
    {
        var secret = Encoding.ASCII.GetBytes("12345678901234567890");
        Assert.Equal(expected, Totp.Code(secret, Totp.StepAt(DateTimeOffset.FromUnixTimeSeconds(unixTime)), digits: 8));
    }

    [Fact]
    public void A_code_from_a_phone_a_little_out_of_time_is_accepted_but_an_old_one_is_not()
    {
        var secret = Totp.NewSecret();
        var now = DateTimeOffset.UtcNow;
        Assert.NotNull(Totp.Match(secret, Totp.Code(secret, Totp.StepAt(now) - 1), now));
        Assert.Null(Totp.Match(secret, Totp.Code(secret, Totp.StepAt(now) - 3), now));
    }

    [Fact]
    public void Base32_matches_the_standard_and_round_trips()
    {
        Assert.Equal("MZXW6YTBOI", Base32.Encode(Encoding.ASCII.GetBytes("foobar")));
        var secret = Totp.NewSecret();
        Assert.Equal(secret, Base32.Decode(Base32.Encode(secret)));
    }

    // ---- secrets are encrypted at rest ----

    private static TwoFactorProtector Protector(string key = "0123456789abcdef0123456789abcdef") =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = key }).Build());

    [Fact]
    public void Stored_secrets_are_encrypted_and_tamper_proof()
    {
        var secret = Totp.NewSecret();
        var stored = Protector().Protect(secret);

        Assert.DoesNotContain(Base32.Encode(secret), stored);
        Assert.Equal(secret, Protector().Unprotect(stored));
        Assert.ThrowsAny<Exception>(() => Protector("another-key-another-key-another-key").Unprotect(stored));
        var tampered = stored[..^4] + (stored[^4] == 'A' ? "B" : "A") + stored[^3..];
        Assert.ThrowsAny<Exception>(() => Protector().Unprotect(tampered));
    }

    // ---- the service ----

    private sealed class FakeUsers : IUserRepository
    {
        public readonly User User = new() { Email = "owner@school", FullName = "Owner", Role = "Teacher", Username = "owner" };
        public Task<User?> FindByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult<User?>(id == User.Id ? User : null);
        public Task<int> SetTwoFactorAsync(Guid userId, string? secret, bool enabled, string? codes, CancellationToken ct = default)
        { User.TwoFactorSecret = secret; User.TwoFactorEnabled = enabled; User.TwoFactorRecoveryCodes = codes; User.TwoFactorLastStep = null; return Task.FromResult(1); }
        public Task<bool> TryAdvanceTwoFactorStepAsync(Guid userId, long step, CancellationToken ct = default)
        { if (User.TwoFactorLastStep is { } last && last >= step) return Task.FromResult(false); User.TwoFactorLastStep = step; return Task.FromResult(true); }
        public Task<bool> TryReplaceRecoveryCodesAsync(Guid userId, string expected, string? remaining, CancellationToken ct = default)
        { if (User.TwoFactorRecoveryCodes != expected) return Task.FromResult(false); User.TwoFactorRecoveryCodes = remaining; return Task.FromResult(true); }
        public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<User?> FindByLoginAsync(string loginId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<User>> SearchUsersAsync(string? role, string? keyword, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> CountUsersAsync(string? role, string? keyword, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddAsync(User user, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> SetActiveStatusAsync(Guid userId, bool isActive, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> SetPasswordHashAsync(Guid userId, string passwordHash, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> IncrementFailedLoginAttemptsAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> SetLockoutAsync(Guid userId, DateTime lockoutEndUtc, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> ResetFailedLoginAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task HardDeleteAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> SetMustChangePasswordAsync(Guid userId, bool mustChange, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> AnonymizeAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class NoAudit : IAuditTrail { public void Record(AuditRecord record) { } }

    private static (TwoFactorService Service, FakeUsers Users) Create(params string[] requiredRoles) =>
        Create(new FakeUsers(), "0123456789abcdef0123456789abcdef", requiredRoles);

    private static (TwoFactorService Service, FakeUsers Users) Create(FakeUsers users, string jwtKey, params string[] requiredRoles)
    {
        users.User.PasswordHash ??= new PasswordHasher<User>().HashPassword(users.User, "correct horse battery");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = jwtKey, ["School:Name"] = "GKMPS" }).Build();
        var service = new TwoFactorService(users, new TwoFactorProtector(config), new NoAudit(), TimeProvider.System,
            Options.Create(new SecurityOptions { RequireTwoFactorForRoles = requiredRoles }), config);
        return (service, users);
    }

    private static string CurrentCode(TwoFactorSetup setup) =>
        Totp.Code(Base32.Decode(setup.SecretKey.Replace(" ", "")), Totp.StepAt(DateTimeOffset.UtcNow));

    [Fact]
    public async Task Setting_up_needs_a_working_code_and_gives_ten_recovery_codes()
    {
        var (twoFactor, users) = Create();
        var setup = await twoFactor.BeginSetupAsync(users.User.Id);
        Assert.StartsWith("otpauth://totp/GKMPS:owner?secret=", setup.SetupUri);

        await Assert.ThrowsAsync<InvalidOperationException>(() => twoFactor.EnableAsync(users.User.Id, "000000"));
        var codes = await twoFactor.EnableAsync(users.User.Id, CurrentCode(setup));

        Assert.True(users.User.TwoFactorEnabled);
        Assert.Equal(10, codes.Count);
        Assert.Equal(10, codes.Distinct().Count());
    }

    [Fact]
    public async Task Each_code_works_only_once()
    {
        var (twoFactor, users) = Create();
        var setup = await twoFactor.BeginSetupAsync(users.User.Id);
        var secret = Base32.Decode(setup.SecretKey.Replace(" ", ""));
        var step = Totp.StepAt(DateTimeOffset.UtcNow);
        await twoFactor.EnableAsync(users.User.Id, Totp.Code(secret, step - 1));

        var code = Totp.Code(secret, step);
        Assert.True(await twoFactor.VerifyAsync(users.User, code));
        Assert.False(await twoFactor.VerifyAsync(users.User, code));   // replayed
    }

    [Fact]
    public async Task A_recovery_code_works_once_when_the_phone_is_lost()
    {
        var (twoFactor, users) = Create();
        var setup = await twoFactor.BeginSetupAsync(users.User.Id);
        var codes = await twoFactor.EnableAsync(users.User.Id, CurrentCode(setup));

        Assert.True(await twoFactor.VerifyAsync(users.User, codes[3].ToLowerInvariant().Replace("-", " ")));
        Assert.False(await twoFactor.VerifyAsync(users.User, codes[3]));
        Assert.Equal(9, (await twoFactor.GetStatusAsync(users.User.Id)).RecoveryCodesLeft);
    }

    [Fact]
    public async Task After_the_signing_key_changes_recovery_codes_still_sign_in()
    {
        var (twoFactor, users) = Create();
        var setup = await twoFactor.BeginSetupAsync(users.User.Id);
        var codes = await twoFactor.EnableAsync(users.User.Id, CurrentCode(setup));

        // The JWT signing key was rotated after an incident, with no separate two-step key set.
        var (rotated, _) = Create(users, "a-brand-new-signing-key-after-an-incident");

        Assert.False(await rotated.VerifyAsync(users.User, CurrentCode(setup)));   // no crash, just no match
        Assert.True(await rotated.VerifyAsync(users.User, codes[0]));
    }

    [Fact]
    public void A_dedicated_two_factor_key_survives_signing_key_rotation()
    {
        TwoFactorProtector With(string jwt) => new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["Jwt:SigningKey"] = jwt, ["TwoFactor:EncryptionKey"] = "a-separate-key-kept-just-for-two-step-secrets" }).Build());
        var secret = Totp.NewSecret();

        var stored = With("the-old-signing-key-0123456789abcdef").Protect(secret);

        Assert.Equal(secret, With("the-new-signing-key-0123456789abcdef").TryUnprotect(stored));
        Assert.Null(Protector().TryUnprotect(stored));
    }

    [Fact]
    public async Task Turning_it_off_needs_the_password_and_a_code_and_is_refused_where_required()
    {
        var (twoFactor, users) = Create();
        var setup = await twoFactor.BeginSetupAsync(users.User.Id);
        var codes = await twoFactor.EnableAsync(users.User.Id, CurrentCode(setup));

        await Assert.ThrowsAsync<InvalidOperationException>(() => twoFactor.DisableAsync(users.User.Id, "wrong password", codes[0]));
        await twoFactor.DisableAsync(users.User.Id, "correct horse battery", codes[1]);
        Assert.False(users.User.TwoFactorEnabled);

        var (required, owner) = Create("Teacher");
        var s2 = await required.BeginSetupAsync(owner.User.Id);
        var c2 = await required.EnableAsync(owner.User.Id, CurrentCode(s2));
        await Assert.ThrowsAsync<InvalidOperationException>(() => required.DisableAsync(owner.User.Id, "correct horse battery", c2[0]));
    }
}

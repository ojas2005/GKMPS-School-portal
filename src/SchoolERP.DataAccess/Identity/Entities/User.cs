using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Identity.Entities;

/// <summary>
/// A single account record covering every role (SuperAdmin/Principal/Admin/Teacher/
/// Student/Parent/Accountant/Librarian). Role-specific profile data (e.g. a Student's
/// admission record) lives in the owning module (Student, Staff, ...) and is linked back to
/// this Id -- the Identity module never stores domain profile fields.
/// </summary>
public class User : BaseEntity
{
    public required string Email { get; set; }

    /// <summary>Optional short login id (e.g. "ownerishim", "adm1042") handed out by the
    /// school owner. Login accepts either this or the email; unique when present.</summary>
    public string? Username { get; set; }

    /// <summary>PBKDF2+HMAC-SHA256 hash produced by ASP.NET Core's PasswordHasher&lt;User&gt;.</summary>
    public string? PasswordHash { get; set; }

    public required string FullName { get; set; }

    /// <summary>One of SchoolERP.Common.RoleNames.</summary>
    public required string Role { get; set; }

    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Consecutive failed password attempts since the last success or lockout
    /// reset. Reset to 0 on a successful login.</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>Set once <see cref="FailedLoginAttempts"/> crosses the threshold; login is
    /// refused (even with the correct password) until this passes.</summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>Id of the linked profile in the owning module's database (StudentId, StaffId, ...). Not an EF navigation -- each module has its own database, so it is looked up through that module, never joined.</summary>
    public Guid? LinkedProfileId { get; set; }

    /// <summary>
    /// Set when someone else chose this password (an admin creating or resetting the
    /// account, or the owner seed): the user must pick their own before using the app.
    /// </summary>
    public bool MustChangePassword { get; set; }

    /// <summary>Authenticator-app secret, encrypted (see TwoFactorProtector). Set during setup, before it's enabled.</summary>
    public string? TwoFactorSecret { get; set; }
    public bool TwoFactorEnabled { get; set; }
    /// <summary>The last time-step a code was accepted for -- each code works once.</summary>
    public long? TwoFactorLastStep { get; set; }
    /// <summary>SHA-256 hashes of the unused recovery codes, ';'-separated.</summary>
    public string? TwoFactorRecoveryCodes { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

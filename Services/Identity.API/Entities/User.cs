using SchoolERP.Shared.Entities;

namespace SchoolERP.Identity.Entities;

/// <summary>
/// A single account record covering every role (SuperAdmin/Principal/Admin/Teacher/
/// Student/Parent/Accountant/Librarian). Role-specific profile data (e.g. a Student's
/// admission record) lives in the owning service (Student.API, Staff.API, ...) and is
/// linked back to this Id -- Identity.API never stores domain profile fields.
/// </summary>
public class User : BaseEntity
{
    public required string Email { get; set; }

    /// <summary>Optional short login id (e.g. "ownerishim", "adm1042") handed out by the
    /// school owner. Login accepts either this or the email; unique when present.</summary>
    public string? Username { get; set; }

    /// <summary>PBKDF2+HMAC-SHA256 hash produced by ASP.NET Core's PasswordHasher&lt;User&gt;. Null for Google-only accounts.</summary>
    public string? PasswordHash { get; set; }

    public required string FullName { get; set; }

    /// <summary>One of SchoolERP.Shared.Common.RoleNames.</summary>
    public required string Role { get; set; }

    public bool IsEmailVerified { get; set; } = false;
    public bool IsActive { get; set; } = true;

    /// <summary>Set when the account was created via "Sign in with Google" rather than a password.</summary>
    public string? GoogleSubjectId { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    /// <summary>Consecutive failed password attempts since the last success or lockout
    /// reset. Reset to 0 on a successful login.</summary>
    public int FailedLoginAttempts { get; set; } = 0;

    /// <summary>Set once <see cref="FailedLoginAttempts"/> crosses the threshold; login is
    /// refused (even with the correct password) until this passes.</summary>
    public DateTime? LockoutEndUtc { get; set; }

    /// <summary>Foreign key into the owning domain service's own table (StudentId, TeacherId, ...). Not an EF navigation -- cross-service FKs are looked up over HTTP, never joined.</summary>
    public Guid? LinkedProfileId { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

using System.ComponentModel.DataAnnotations;

namespace SchoolERP.Identity.DTOs;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required] string FullName,
    [Required] string Role,
    // Optional short login id the owner hands to the student/teacher (e.g. "adm1042").
    string? Username = null);

public record LoginRequest(
    // Username OR email — the owner hands out short login ids, staff may use email.
    [Required] string LoginId,
    [Required] string Password);

// AccessToken is optional: the frontend keeps it in memory only, so after a page reload
// the refresh token (a 256-bit random value, stored hashed) is all it has. When the old
// access token IS supplied, it must belong to the same user as the refresh token.
public record RefreshRequest(string? AccessToken, [Required] string RefreshToken);

public record LogoutRequest([Required] string RefreshToken);

public record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(8)] string NewPassword);

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    // Present only for student/parent accounts linked to a student record.
    Guid? StudentId = null,
    string? ClassId = null,
    string? SectionId = null,
    string? Username = null,
    // Present only for staff accounts linked to a staff record.
    Guid? StaffId = null,
    string? ClassTeacherOfClassId = null,
    string? ClassTeacherOfSectionId = null);

public record UserSummary(Guid Id, string Email, string FullName, string Role, bool IsActive, DateTime? LastLoginAtUtc, string? Username = null);

public record SetPasswordRequest([Required, MinLength(8)] string NewPassword);

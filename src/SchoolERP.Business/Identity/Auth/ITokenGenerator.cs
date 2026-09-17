using System.Security.Claims;
using SchoolERP.DataAccess.Identity.Entities;

namespace SchoolERP.Business.Identity.Auth;

public interface ITokenGenerator
{
    string GenerateAccessToken(User user, StudentProfile? studentProfile = null, StaffClaimsProfile? staffProfile = null);

    /// <summary>Cryptographically random opaque refresh token (the raw value is only ever returned to the client once; only its hash is persisted).</summary>
    string GenerateRefreshTokenRaw();

    string HashToken(string rawToken);

    ClaimsPrincipal? ValidateAccessTokenIgnoringExpiry(string accessToken);
}

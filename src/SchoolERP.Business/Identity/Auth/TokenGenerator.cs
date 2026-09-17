using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolERP.DataAccess.Identity.Entities;

namespace SchoolERP.Business.Identity.Auth;

public class TokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _options;

    public TokenGenerator(IOptions<JwtOptions> options) => _options = options.Value;

    public string GenerateAccessToken(User user, StudentProfile? studentProfile = null, StaffClaimsProfile? staffProfile = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Self-service scoping claims: present only for accounts linked to a student
        // record. Downstream services use these to restrict reads to the caller's own data.
        if (studentProfile is not null)
        {
            claims.Add(new Claim(SchoolERP.Common.CallerClaims.StudentIdClaim, studentProfile.StudentId.ToString()));
            claims.Add(new Claim(SchoolERP.Common.CallerClaims.ClassIdClaim, studentProfile.ClassId));
            claims.Add(new Claim(SchoolERP.Common.CallerClaims.SectionIdClaim, studentProfile.SectionId));
        }

        // Staff scoping claims: present only for accounts linked to a staff record.
        // classTeacherOf* gate attendance uploads and student-enquiry reads to the
        // teacher's own class; staffId scopes payout/attendance self-reads.
        if (staffProfile is not null)
        {
            claims.Add(new Claim(SchoolERP.Common.CallerClaims.StaffIdClaim, staffProfile.StaffId.ToString()));
            if (!string.IsNullOrEmpty(staffProfile.ClassTeacherOfClassId))
                claims.Add(new Claim(SchoolERP.Common.CallerClaims.ClassTeacherOfClassIdClaim, staffProfile.ClassTeacherOfClassId));
            if (!string.IsNullOrEmpty(staffProfile.ClassTeacherOfSectionId))
                claims.Add(new Claim(SchoolERP.Common.CallerClaims.ClassTeacherOfSectionIdClaim, staffProfile.ClassTeacherOfSectionId));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshTokenRaw()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateAccessTokenIgnoringExpiry(string accessToken)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = false // caller is explicitly refreshing an expired token
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(accessToken, parameters, out var validatedToken);
            if (validatedToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return principal;
        }
        catch
        {
            return null;
        }
    }
}

using Npgsql;

namespace SchoolERP.Identity.Auth;

public record StaffClaimsProfile(Guid StaffId, string? ClassTeacherOfClassId, string? ClassTeacherOfSectionId);

/// <summary>
/// Resolves the Staff profile linked to a user account so Identity can embed
/// staffId/classTeacherOfClassId/classTeacherOfSectionId claims in the access token
/// (used by Attendance.API to let only the class teacher of class X mark class X, and
/// by Staff.API to scope payout/attendance reads to the caller's own record). Reads
/// the staff schema directly, mirroring StudentProfileResolver. Best-effort: any
/// failure returns null so login is never blocked.
/// </summary>
public class StaffProfileResolver
{
    private readonly string? _connectionString;
    private readonly ILogger<StaffProfileResolver> _logger;

    public StaffProfileResolver(IConfiguration configuration, ILogger<StaffProfileResolver> logger)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb");
        _logger = logger;
    }

    public async Task<StaffClaimsProfile?> ResolveByUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_connectionString)) return null;

        try
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new NpgsqlCommand(
                "SELECT \"Id\", \"ClassTeacherOfClassId\", \"ClassTeacherOfSectionId\" " +
                "FROM staff.\"Staff\" " +
                "WHERE \"LinkedUserId\" = @uid AND \"IsDeleted\" = false " +
                "LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return new StaffClaimsProfile(
                    reader.GetGuid(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2));
            }
        }
        catch (Exception ex)
        {
            // Never block login on profile enrichment (e.g. staff schema not migrated yet).
            _logger.LogWarning(ex, "Could not resolve staff profile for user {UserId}", userId);
        }

        return null;
    }
}

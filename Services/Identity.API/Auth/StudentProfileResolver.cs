using MySqlConnector;

namespace SchoolERP.Identity.Auth;

public record StudentProfile(Guid StudentId, string ClassId, string SectionId);

/// <summary>
/// Resolves the Student profile linked to a user account so Identity can embed
/// studentId/classId/sectionId claims in the access token. Reads the `student` database
/// directly via a cross-database query (every service's database lives on the same TiDB
/// cluster, reachable from any service's connection string with a fully-qualified
/// `database`.`table` reference). Best-effort: any failure returns null so login is
/// never blocked (staff accounts simply have no student profile).
/// </summary>
public class StudentProfileResolver
{
    private readonly string? _connectionString;
    private readonly ILogger<StudentProfileResolver> _logger;

    public StudentProfileResolver(IConfiguration configuration, ILogger<StudentProfileResolver> logger)
    {
        _connectionString = configuration.GetConnectionString("IdentityDb");
        _logger = logger;
    }

    /// <param name="asParent">True for Parent-role accounts: match the student whose
    /// ParentUserId is this user (their child) instead of the student's own login.</param>
    public async Task<StudentProfile?> ResolveByUserAsync(Guid userId, bool asParent = false, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_connectionString)) return null;

        try
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new MySqlCommand(
                "SELECT `Id`, `ClassId`, `SectionId` " +
                "FROM student.`Students` " +
                (asParent ? "WHERE `ParentUserId` = @uid " : "WHERE `LinkedUserId` = @uid ") +
                "AND `IsDeleted` = 0 " +
                "LIMIT 1",
                conn);
            cmd.Parameters.AddWithValue("uid", userId);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return new StudentProfile(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2));
            }
        }
        catch (Exception ex)
        {
            // Never block login on profile enrichment (e.g. student schema not migrated yet).
            _logger.LogWarning(ex, "Could not resolve student profile for user {UserId}", userId);
        }

        return null;
    }
}

using MySqlConnector;

namespace SchoolERP.DataAccess;

/// <summary>
/// A quick "is anything to migrate?" check for startup. EF Core's Migrate() costs about a second
/// per database even when there's nothing to do (a lock, a CREATE TABLE IF NOT EXISTS, a
/// history read, an unlock -- each a round trip to the database server), which made waking the
/// app from zero take ~14 s longer than it needed to. This reads every module's migration
/// history in one query per server instead, so Migrate() only runs where something is pending.
/// </summary>
public static class MigrationCheck
{
    /// <summary>
    /// Applied migration IDs per database, read with one UNION query per database server.
    /// Throws if any database or history table is missing (a first deploy) -- callers then
    /// fall back to running Migrate() everywhere.
    /// </summary>
    public static Dictionary<string, HashSet<string>> ReadAppliedMigrations(IConfiguration configuration, IEnumerable<string> databases)
    {
        var applied = new Dictionary<string, HashSet<string>>();

        // Modules can point at different servers (ConnectionStrings:{Module}Db), so ask each once.
        var byServer = databases.GroupBy(database =>
            new MySqlConnectionStringBuilder(DataAccessServiceCollectionExtensions.ConnectionStringFor(configuration, database)) { Database = "" }.ConnectionString);

        foreach (var server in byServer)
        {
            using var connection = new MySqlConnection(server.Key);
            connection.Open();
            using var command = connection.CreateCommand();
            // Database names come from the fixed module list, never from user input.
            command.CommandText = string.Join(" UNION ALL ",
                server.Select(database => $"SELECT '{database}', `MigrationId` FROM `{database}`.`__EFMigrationsHistory`"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var database = reader.GetString(0);
                if (!applied.TryGetValue(database, out var ids))
                    applied[database] = ids = [];
                ids.Add(reader.GetString(1));
            }
        }

        return applied;
    }

    /// <summary>The databases with at least one migration in code that hasn't been applied yet.</summary>
    public static HashSet<string> DatabasesWithPendingMigrations(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> expected,
        IReadOnlyDictionary<string, HashSet<string>> applied) =>
        expected
            .Where(module => module.Value.Any(id => !applied.TryGetValue(module.Key, out var done) || !done.Contains(id)))
            .Select(module => module.Key)
            .ToHashSet();
}

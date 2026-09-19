using SchoolERP.DataAccess;

namespace SchoolERP.Tests;

public class MigrationCheckTests
{
    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Expected(params (string Db, string[] Ids)[] modules) =>
        modules.ToDictionary(m => m.Db, m => (IReadOnlyCollection<string>)m.Ids);

    private static IReadOnlyDictionary<string, HashSet<string>> Applied(params (string Db, string[] Ids)[] modules) =>
        modules.ToDictionary(m => m.Db, m => m.Ids.ToHashSet());

    [Fact]
    public void Nothing_to_do_when_every_database_is_current()
    {
        var pending = MigrationCheck.DatabasesWithPendingMigrations(
            Expected(("identity", ["001", "002"]), ("fee", ["001"])),
            Applied(("identity", ["001", "002"]), ("fee", ["001"])));

        Assert.Empty(pending);
    }

    [Fact]
    public void Only_the_database_with_a_new_migration_is_migrated()
    {
        var pending = MigrationCheck.DatabasesWithPendingMigrations(
            Expected(("identity", ["001", "002"]), ("fee", ["001"])),
            Applied(("identity", ["001"]), ("fee", ["001"])));

        Assert.Equal(["identity"], pending);
    }

    [Fact]
    public void A_database_with_no_history_yet_is_migrated()
    {
        var pending = MigrationCheck.DatabasesWithPendingMigrations(
            Expected(("identity", ["001"]), ("files", ["001"])),
            Applied(("identity", ["001"])));

        Assert.Equal(["files"], pending);
    }

    [Fact]
    public void Migrations_applied_by_a_newer_version_dont_count_as_pending()
    {
        // Rolling back to an older image: the database is ahead, not behind.
        var pending = MigrationCheck.DatabasesWithPendingMigrations(
            Expected(("identity", ["001"])),
            Applied(("identity", ["001", "002"])));

        Assert.Empty(pending);
    }
}

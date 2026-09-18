using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolERP.Common;
using SchoolERP.Common.Hosting;
using SchoolERP.DataAccess;
using SchoolERP.DataAccess.Identity;
using SchoolERP.DataAccess.Identity.Entities;
using Serilog;

namespace SchoolERP.Api.Startup;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending EF Core migrations for every module's database (retrying while the
    /// database comes up), then seeds the school-owner account on first boot.
    /// </summary>
    public static void Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var contexts = DataAccessServiceCollectionExtensions.Modules
            .ToDictionary(m => m.Database, m => (DbContext)scope.ServiceProvider.GetRequiredService(m.ContextType));

        foreach (var database in DatabasesToMigrate(app, contexts))
        {
            var db = contexts[database];
            SharedHosting.MigrateWithRetry(() => db.Database.Migrate(), app.Logger);
            app.Logger.LogInformation("Database '{Database}' is up to date", database);
        }

        SeedOwner(app, scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
    }

    // Every startup -- including each wake from scale-to-zero -- used to run Migrate() on all
    // 13 databases, about a second each even with nothing to apply. One history query answers
    // "is anything pending?" instead; only databases that need it go through Migrate(). If the
    // quick check can't run (first deploy, a database still starting), everything takes the
    // full path as before, which creates, retries and migrates.
    private static IReadOnlyCollection<string> DatabasesToMigrate(WebApplication app, IReadOnlyDictionary<string, DbContext> contexts)
    {
        var expected = contexts.ToDictionary(c => c.Key, c => (IReadOnlyCollection<string>)c.Value.Database.GetMigrations().ToList());
        try
        {
            var applied = MigrationCheck.ReadAppliedMigrations(app.Configuration, contexts.Keys);
            var pending = MigrationCheck.DatabasesWithPendingMigrations(expected, applied);
            app.Logger.LogInformation("Checked {Count} databases in one query; {Pending} need migrating", contexts.Count, pending.Count);
            return DataAccessServiceCollectionExtensions.Modules.Select(m => m.Database).Where(pending.Contains).ToList();
        }
        catch (Exception ex)
        {
            app.Logger.LogInformation("Quick migration check unavailable ({Reason}); checking each database", ex.Message);
            return DataAccessServiceCollectionExtensions.Modules.Select(m => m.Database).ToList();
        }
    }

    // Registration is closed to the public, so the owner (SuperAdmin) is the bootstrap identity
    // every other account is created from. Credentials come from Owner:Username/Owner:Password;
    // with no password configured, a random one is generated and logged once -- never a
    // hard-coded default that would live forever in git history.
    private static void SeedOwner(WebApplication app, IdentityDbContext db)
    {
        var ownerUsername = app.Configuration["Owner:Username"] ?? "ownerishim";
        if (db.Users.IgnoreQueryFilters().Any(u => u.Username == ownerUsername))
            return;

        var ownerPassword = app.Configuration["Owner:Password"];
        var passwordWasGenerated = string.IsNullOrEmpty(ownerPassword);
        if (passwordWasGenerated)
            ownerPassword = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(18));

        var owner = new User
        {
            Email = $"{ownerUsername}@gkmps.local",
            Username = ownerUsername,
            FullName = "School Owner",
            Role = RoleNames.SuperAdmin,
            IsEmailVerified = true,
            IsActive = true
        };
        owner.PasswordHash = new PasswordHasher<User>().HashPassword(owner, ownerPassword!);
        db.Users.Add(owner);
        db.SaveChanges();

        if (passwordWasGenerated)
            Log.Warning("Seeded school-owner account '{Username}' with generated password '{Password}' -- log in and change it now, this is the only place it appears", ownerUsername, ownerPassword);
        else
            Log.Information("Seeded school-owner account '{Username}'", ownerUsername);
    }
}

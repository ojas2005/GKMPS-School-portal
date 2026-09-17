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

        foreach (var (contextType, database) in DataAccessServiceCollectionExtensions.Modules)
        {
            var db = (DbContext)scope.ServiceProvider.GetRequiredService(contextType);
            SharedHosting.MigrateWithRetry(() => db.Database.Migrate(), app.Logger);
            app.Logger.LogInformation("Database '{Database}' is up to date", database);
        }

        SeedOwner(app, scope.ServiceProvider.GetRequiredService<IdentityDbContext>());
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

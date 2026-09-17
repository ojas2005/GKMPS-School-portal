using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Transport.Entities;
using SchoolERP.Common.Entities;
using RouteEntity = SchoolERP.DataAccess.Transport.Entities.Route;

namespace SchoolERP.DataAccess.Transport;

public class TransportDbContext : DbContext
{
    public TransportDbContext(DbContextOptions<TransportDbContext> options) : base(options) { }

    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<StudentRouteMapping> StudentRouteMappings => Set<StudentRouteMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasIndex(v => v.RegistrationNumber).IsUnique();
        });

        modelBuilder.Entity<StudentRouteMapping>(entity =>
        {
            entity.HasIndex(m => m.StudentId).IsUnique();
            entity.HasIndex(m => m.RouteId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });

        // Pomelo defaults Guid columns to collation "ascii_general_ci", which TiDB's new
        // collation framework doesn't support (only ascii_bin is in its allowed set).
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(Guid) || property.ClrType == typeof(Guid?))
                {
                    property.SetCollation("ascii_bin");
                }
            }
        }
    }
}

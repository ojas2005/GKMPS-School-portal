using Microsoft.EntityFrameworkCore;
using SchoolERP.Transport.Entities;
using SchoolERP.Shared.Entities;
using RouteEntity = SchoolERP.Transport.Entities.Route;

namespace SchoolERP.Transport.Data;

public class TransportDbContext : DbContext
{
    public TransportDbContext(DbContextOptions<TransportDbContext> options) : base(options) { }

    public DbSet<RouteEntity> Routes => Set<RouteEntity>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<StudentRouteMapping> StudentRouteMappings => Set<StudentRouteMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("transport");

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
    }
}

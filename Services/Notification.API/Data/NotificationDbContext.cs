using Microsoft.EntityFrameworkCore;
using SchoolERP.Notification.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Notification.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");

        modelBuilder.Entity<NotificationLog>(entity =>
        {
            entity.HasIndex(n => new { n.EventType, n.CreatedAtUtc });
            entity.Property(n => n.PayloadJson).HasColumnType("jsonb");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}

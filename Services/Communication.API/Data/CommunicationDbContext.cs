using Microsoft.EntityFrameworkCore;
using SchoolERP.Communication.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Communication.Data;

public class CommunicationDbContext : DbContext
{
    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options) : base(options) { }

    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<ParentMessage> ParentMessages => Set<ParentMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("communication");

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasIndex(a => a.PublishedAtUtc);
        });

        modelBuilder.Entity<ParentMessage>(entity =>
        {
            entity.HasIndex(m => new { m.StudentId, m.CreatedAtUtc });
            entity.HasIndex(m => new { m.RecipientUserId, m.IsRead });
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}

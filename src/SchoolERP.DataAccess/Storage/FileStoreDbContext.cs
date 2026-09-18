using Microsoft.EntityFrameworkCore;

namespace SchoolERP.DataAccess.Storage;

/// <summary>
/// Generated PDFs, stored in their own "files" database on the same server as the modules.
/// They're small (tens of KB each), so this replaces a paid storage account at no cost.
/// </summary>
public class FileStoreDbContext : DbContext
{
    public FileStoreDbContext(DbContextOptions<FileStoreDbContext> options) : base(options) { }

    public DbSet<StoredFile> Files => Set<StoredFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<StoredFile>(entity =>
        {
            entity.ToTable("StoredFiles");
            entity.Property(f => f.Container).HasMaxLength(63);
            entity.Property(f => f.Path).HasMaxLength(300);
            entity.Property(f => f.ContentType).HasMaxLength(100);
            entity.Property(f => f.Content).HasColumnType("longblob");
            entity.HasIndex(f => new { f.Container, f.Path }).IsUnique();

            // Same TiDB collation workaround as the module contexts: Guid columns default to
            // ascii_general_ci, which TiDB's new collation framework doesn't support.
            entity.Property(f => f.Id).UseCollation("ascii_bin");
        });
    }
}

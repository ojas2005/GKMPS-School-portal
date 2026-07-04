using Microsoft.EntityFrameworkCore;
using SchoolERP.Library.Entities;
using SchoolERP.Shared.Entities;

namespace SchoolERP.Library.Data;

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("library");

        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasIndex(b => b.Isbn).IsUnique();
            entity.Property(b => b.Isbn).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<BookIssue>(entity =>
        {
            entity.Ignore(i => i.IsReturned); // computed, not persisted
            entity.HasIndex(i => new { i.StudentId, i.BookId, i.ReturnedAtUtc });
            entity.HasOne(i => i.Book)
                  .WithMany()
                  .HasForeignKey(i => i.BookId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasIndex(a => new { a.EntityName, a.EntityId });
        });
    }
}

using Microsoft.EntityFrameworkCore;
using SchoolERP.DataAccess.Fee.Entities;
using SchoolERP.Common.Entities;

namespace SchoolERP.DataAccess.Fee;

public class FeeDbContext : DbContext
{
    public FeeDbContext(DbContextOptions<FeeDbContext> options) : base(options) { }

    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeePayment> FeePayments => Set<FeePayment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<ClassFeeRate> ClassFeeRates => Set<ClassFeeRate>();
    public DbSet<FeeSchedule> FeeSchedules => Set<FeeSchedule>();
    public DbSet<StudentFeeAccount> StudentFeeAccounts => Set<StudentFeeAccount>();
    public DbSet<FeeAdjustment> FeeAdjustments => Set<FeeAdjustment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("utf8mb4_general_ci");

        modelBuilder.Entity<FeeStructure>(entity =>
        {
            entity.HasIndex(f => new { f.ClassId, f.AcademicYear });
        });

        modelBuilder.Entity<FeePayment>(entity =>
        {
            // MySQL/TiDB treats NULLs as distinct in a unique index, so charges added by hand
            // (no FeeStructureId, no PeriodKey) can repeat; generated fees can't.
            entity.HasIndex(p => new { p.StudentId, p.FeeStructureId }).IsUnique();
            entity.HasIndex(p => new { p.StudentId, p.Category, p.AcademicYear, p.PeriodKey }).IsUnique();
            entity.HasIndex(p => p.ClassId); // backs the owner's pending-by-class summary
            entity.HasIndex(p => p.DueDate);
            entity.HasIndex(p => new { p.Category, p.AcademicYear });
            entity.Property(p => p.ClassId).HasMaxLength(64);
            entity.Property(p => p.Category).HasMaxLength(20).IsRequired().HasDefaultValue(FeeCategories.Other);
            entity.Property(p => p.AcademicYear).HasMaxLength(10);
            entity.Property(p => p.PeriodKey).HasMaxLength(20);
            entity.Property(p => p.PeriodLabel).HasMaxLength(50);
            entity.Property(p => p.Description).HasMaxLength(200);
            entity.Property(p => p.DueDate).HasColumnType("date");
            entity.Ignore(p => p.Status); // computed, not persisted
            entity.HasOne(p => p.FeeStructure)
                  .WithMany()
                  .HasForeignKey(p => p.FeeStructureId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasIndex(t => t.ReceiptNumber).IsUnique();
            entity.HasIndex(t => t.FeePaymentId);
            entity.HasIndex(t => t.PaidAtUtc);
            entity.Property(t => t.ReceiptNumber).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<ClassFeeRate>(entity =>
        {
            entity.HasIndex(r => r.ClassId).IsUnique();
            entity.Property(r => r.ClassId).HasMaxLength(64);
        });

        modelBuilder.Entity<StudentFeeAccount>(entity =>
        {
            entity.HasIndex(a => a.StudentId).IsUnique();
            entity.HasIndex(a => a.IsOpen);
            entity.Property(a => a.BillingStartDate).HasColumnType("date");
            entity.Property(a => a.ClosedReason).HasMaxLength(200);
        });

        modelBuilder.Entity<FeeAdjustment>(entity =>
        {
            entity.HasIndex(a => a.StudentId);
            entity.HasIndex(a => a.FeePaymentId);
            entity.Property(a => a.Kind).HasMaxLength(30);
            entity.Property(a => a.Reason).HasMaxLength(500);
            entity.Property(a => a.ActorRole).HasMaxLength(50);
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

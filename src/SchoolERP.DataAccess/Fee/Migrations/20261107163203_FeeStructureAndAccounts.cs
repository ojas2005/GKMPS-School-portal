using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.DataAccess.Fee.Migrations
{
    /// <inheritdoc />
    public partial class FeeStructureAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FeePayments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "ClassId",
                table: "FeePayments",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "FeePayments",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "FeePayments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Other",
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "FeePayments",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPaidAtUtc",
                table: "FeePayments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PeriodKey",
                table: "FeePayments",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "ClassFeeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    ClassId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false, collation: "utf8mb4_general_ci"),
                    AdmissionFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    MonthlyTuition = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    QuarterlyExamFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    HalfYearlyExamFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    AnnualExamFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassFeeRates", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "FeeAdjustments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    FeePaymentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_bin"),
                    Kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, collation: "utf8mb4_general_ci"),
                    Amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_general_ci"),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_bin"),
                    ActorRole = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, collation: "utf8mb4_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeAdjustments", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "FeeSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    MonthlyDueDay = table.Column<int>(type: "int", nullable: false),
                    QuarterlyExamDueMonth = table.Column<int>(type: "int", nullable: false),
                    QuarterlyExamDueDay = table.Column<int>(type: "int", nullable: false),
                    HalfYearlyExamDueMonth = table.Column<int>(type: "int", nullable: false),
                    HalfYearlyExamDueDay = table.Column<int>(type: "int", nullable: false),
                    AnnualExamDueMonth = table.Column<int>(type: "int", nullable: false),
                    AnnualExamDueDay = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeeSchedules", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "StudentFeeAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    BillingStartDate = table.Column<DateTime>(type: "date", nullable: false),
                    IsOpen = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedReason = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true, collation: "utf8mb4_general_ci"),
                    LastGeneratedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentFeeAccounts", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaidAtUtc",
                table: "PaymentTransactions",
                column: "PaidAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_Category_AcademicYear",
                table: "FeePayments",
                columns: new[] { "Category", "AcademicYear" });

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_DueDate",
                table: "FeePayments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_StudentId_Category_AcademicYear_PeriodKey",
                table: "FeePayments",
                columns: new[] { "StudentId", "Category", "AcademicYear", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassFeeRates_ClassId",
                table: "ClassFeeRates",
                column: "ClassId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeAdjustments_FeePaymentId",
                table: "FeeAdjustments",
                column: "FeePaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FeeAdjustments_StudentId",
                table: "FeeAdjustments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAccounts_IsOpen",
                table: "StudentFeeAccounts",
                column: "IsOpen");

            migrationBuilder.CreateIndex(
                name: "IX_StudentFeeAccounts_StudentId",
                table: "StudentFeeAccounts",
                column: "StudentId",
                unique: true);

            // Existing fees: give them a due date, a session and the date they were last paid,
            // so they appear correctly in the new lists and totals. Fees from the older class fee
            // structures take that structure's due date and year; charges added by hand were
            // payable when they were added.
            migrationBuilder.Sql(@"
UPDATE FeePayments p JOIN FeeStructures s ON p.FeeStructureId = s.Id
SET p.DueDate = DATE(s.DueDateUtc),
    p.AcademicYear = CASE
        WHEN s.AcademicYear REGEXP '^[0-9]{4} *[-/] *([0-9]{2}|[0-9]{4})$' THEN CONCAT(LEFT(s.AcademicYear, 4), '-', RIGHT(s.AcademicYear, 2))
        ELSE LEFT(s.AcademicYear, 10) END
WHERE p.DueDate IS NULL;");
            migrationBuilder.Sql(@"
UPDATE FeePayments SET DueDate = DATE(CreatedAtUtc) WHERE DueDate IS NULL;");
            migrationBuilder.Sql(@"
UPDATE FeePayments
SET AcademicYear = CASE
    WHEN MONTH(DueDate) >= 4 THEN CONCAT(YEAR(DueDate), '-', LPAD(MOD(YEAR(DueDate) + 1, 100), 2, '0'))
    ELSE CONCAT(YEAR(DueDate) - 1, '-', LPAD(MOD(YEAR(DueDate), 100), 2, '0')) END
WHERE AcademicYear IS NULL;");
            migrationBuilder.Sql(@"
UPDATE FeePayments p JOIN (SELECT FeePaymentId, MAX(PaidAtUtc) AS LastPaid FROM PaymentTransactions GROUP BY FeePaymentId) t
    ON t.FeePaymentId = p.Id
SET p.LastPaidAtUtc = t.LastPaid;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassFeeRates");

            migrationBuilder.DropTable(
                name: "FeeAdjustments");

            migrationBuilder.DropTable(
                name: "FeeSchedules");

            migrationBuilder.DropTable(
                name: "StudentFeeAccounts");

            migrationBuilder.DropIndex(
                name: "IX_PaymentTransactions_PaidAtUtc",
                table: "PaymentTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FeePayments_Category_AcademicYear",
                table: "FeePayments");

            migrationBuilder.DropIndex(
                name: "IX_FeePayments_DueDate",
                table: "FeePayments");

            migrationBuilder.DropIndex(
                name: "IX_FeePayments_StudentId_Category_AcademicYear_PeriodKey",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "LastPaidAtUtc",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "PeriodKey",
                table: "FeePayments");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "FeePayments",
                type: "longtext",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "ClassId",
                table: "FeePayments",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(64)",
                oldMaxLength: 64,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Attendance.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "attendance");

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ArrivalTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsLate = table.Column<bool>(type: "boolean", nullable: false),
                    MarkedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    BeforeStateJson = table.Column<string>(type: "text", nullable: true),
                    AfterStateJson = table.Column<string>(type: "text", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlySummaries",
                schema: "attendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    PresentCount = table.Column<int>(type: "integer", nullable: false),
                    AbsentCount = table.Column<int>(type: "integer", nullable: false),
                    LateCount = table.Column<int>(type: "integer", nullable: false),
                    TotalMarkedDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlySummaries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ClassId_SectionId_Date",
                schema: "attendance",
                table: "AttendanceRecords",
                columns: new[] { "ClassId", "SectionId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_Date",
                schema: "attendance",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "attendance",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlySummaries_StudentId_Year_Month",
                schema: "attendance",
                table: "MonthlySummaries",
                columns: new[] { "StudentId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceRecords",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "attendance");

            migrationBuilder.DropTable(
                name: "MonthlySummaries",
                schema: "attendance");
        }
    }
}

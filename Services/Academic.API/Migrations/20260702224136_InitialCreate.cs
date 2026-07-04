using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Academic.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "academic");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "academic",
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
                name: "Homeworks",
                schema: "academic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    AssignedDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Homeworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                schema: "academic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    TeacherStaffId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timetables",
                schema: "academic",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "text", nullable: false),
                    SlotsJson = table.Column<string>(type: "jsonb", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timetables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "academic",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Homeworks_ClassId_SectionId_DueDateUtc",
                schema: "academic",
                table: "Homeworks",
                columns: new[] { "ClassId", "SectionId", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_ClassId_Code",
                schema: "academic",
                table: "Subjects",
                columns: new[] { "ClassId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timetables_ClassId_SectionId",
                schema: "academic",
                table: "Timetables",
                columns: new[] { "ClassId", "SectionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "academic");

            migrationBuilder.DropTable(
                name: "Homeworks",
                schema: "academic");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "academic");

            migrationBuilder.DropTable(
                name: "Timetables",
                schema: "academic");
        }
    }
}

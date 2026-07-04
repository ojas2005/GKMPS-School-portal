using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Examination.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "examination");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "examination",
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
                name: "Exams",
                schema: "examination",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxMarks = table.Column<int>(type: "integer", nullable: false),
                    PassingMarks = table.Column<int>(type: "integer", nullable: false),
                    AnswerKeyJson = table.Column<string>(type: "jsonb", nullable: true),
                    IsResultPublished = table.Column<bool>(type: "boolean", nullable: false),
                    ResultPublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarksEntries",
                schema: "examination",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExamId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarksObtained = table.Column<decimal>(type: "numeric", nullable: false),
                    Grade = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    EnteredByStaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarksEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarksEntries_Exams_ExamId",
                        column: x => x.ExamId,
                        principalSchema: "examination",
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "examination",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ClassId_SubjectId_ExamDateUtc",
                schema: "examination",
                table: "Exams",
                columns: new[] { "ClassId", "SubjectId", "ExamDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarksEntries_ExamId_StudentId",
                schema: "examination",
                table: "MarksEntries",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "examination");

            migrationBuilder.DropTable(
                name: "MarksEntries",
                schema: "examination");

            migrationBuilder.DropTable(
                name: "Exams",
                schema: "examination");
        }
    }
}

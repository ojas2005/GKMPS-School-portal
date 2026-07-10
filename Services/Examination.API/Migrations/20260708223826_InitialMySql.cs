using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Examination.Migrations
{
    /// <inheritdoc />
    public partial class InitialMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    ActorUserId = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    ActorRole = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Action = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    EntityName = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    EntityId = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    BeforeStateJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    AfterStateJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    Name = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    ClassId = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    SubjectId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    ExamDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    MaxMarks = table.Column<int>(type: "int", nullable: false),
                    PassingMarks = table.Column<int>(type: "int", nullable: false),
                    AnswerKeyJson = table.Column<string>(type: "json", nullable: true, collation: "utf8mb4_general_ci"),
                    IsResultPublished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ResultPublishedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "MarksEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    ExamId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    MarksObtained = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Grade = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    Remarks = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    EnteredByStaffId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarksEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarksEntries_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Exams_ClassId_SubjectId_ExamDateUtc",
                table: "Exams",
                columns: new[] { "ClassId", "SubjectId", "ExamDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MarksEntries_ExamId_StudentId",
                table: "MarksEntries",
                columns: new[] { "ExamId", "StudentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "MarksEntries");

            migrationBuilder.DropTable(
                name: "Exams");
        }
    }
}

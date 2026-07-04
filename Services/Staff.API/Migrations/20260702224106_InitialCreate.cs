using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Staff.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "staff");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "staff",
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
                name: "Staff",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "text", nullable: false),
                    SubjectsTaughtCsv = table.Column<string>(type: "text", nullable: true),
                    DateOfJoiningUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveType = table.Column<string>(type: "text", nullable: false),
                    FromDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ToDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionNote = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Staff_StaffId",
                        column: x => x.StaffId,
                        principalSchema: "staff",
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "staff",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StaffId_FromDateUtc",
                schema: "staff",
                table: "LeaveRequests",
                columns: new[] { "StaffId", "FromDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_EmployeeCode",
                schema: "staff",
                table: "Staff",
                column: "EmployeeCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "Staff",
                schema: "staff");
        }
    }
}

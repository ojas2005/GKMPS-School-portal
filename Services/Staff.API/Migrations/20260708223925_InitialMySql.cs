using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Staff.Migrations
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
                name: "Staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    LinkedUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    EmployeeCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    FullName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false, collation: "utf8mb4_general_ci"),
                    Designation = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    SubjectsTaughtCsv = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    DateOfJoiningUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Phone = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    Email = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    Status = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    ClassTeacherOfClassId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    ClassTeacherOfSectionId = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    MonthlySalary = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Staff", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StaffId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    LeaveType = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    FromDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ToDateUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    IsSubmitted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsApproved = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsRejected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DecidedByUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_bin"),
                    DecisionNote = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StaffId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    PeriodLabel = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false, collation: "utf8mb4_general_ci"),
                    PaidOnUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Method = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Reference = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    Notes = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    RecordedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "StaffAttendance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StaffId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, collation: "utf8mb4_general_ci"),
                    MarkedByUserId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    MarkedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendance_Staff_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_StaffId_FromDateUtc",
                table: "LeaveRequests",
                columns: new[] { "StaffId", "FromDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StaffId_PaidOnUtc",
                table: "Payouts",
                columns: new[] { "StaffId", "PaidOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Staff_EmployeeCode",
                table: "Staff",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendance_StaffId_Date",
                table: "StaffAttendance",
                columns: new[] { "StaffId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "LeaveRequests");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropTable(
                name: "StaffAttendance");

            migrationBuilder.DropTable(
                name: "Staff");
        }
    }
}

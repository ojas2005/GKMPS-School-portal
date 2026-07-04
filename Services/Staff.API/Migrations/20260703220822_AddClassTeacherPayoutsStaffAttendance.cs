using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Staff.Migrations
{
    /// <inheritdoc />
    public partial class AddClassTeacherPayoutsStaffAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClassTeacherOfClassId",
                schema: "staff",
                table: "Staff",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClassTeacherOfSectionId",
                schema: "staff",
                table: "Staff",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Payouts",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    PeriodLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaidOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Staff_StaffId",
                        column: x => x.StaffId,
                        principalSchema: "staff",
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StaffAttendance",
                schema: "staff",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MarkedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    MarkedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffAttendance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffAttendance_Staff_StaffId",
                        column: x => x.StaffId,
                        principalSchema: "staff",
                        principalTable: "Staff",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_StaffId_PaidOnUtc",
                schema: "staff",
                table: "Payouts",
                columns: new[] { "StaffId", "PaidOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffAttendance_StaffId_Date",
                schema: "staff",
                table: "StaffAttendance",
                columns: new[] { "StaffId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payouts",
                schema: "staff");

            migrationBuilder.DropTable(
                name: "StaffAttendance",
                schema: "staff");

            migrationBuilder.DropColumn(
                name: "ClassTeacherOfClassId",
                schema: "staff",
                table: "Staff");

            migrationBuilder.DropColumn(
                name: "ClassTeacherOfSectionId",
                schema: "staff",
                table: "Staff");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Staff.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlySalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlySalary",
                schema: "staff",
                table: "Staff",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlySalary",
                schema: "staff",
                table: "Staff");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Fee.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeSubmissionPeriodLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PeriodLabel",
                schema: "fee",
                table: "FeePayments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodLabel",
                schema: "fee",
                table: "FeePayments");
        }
    }
}

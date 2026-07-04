using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Fee.Migrations
{
    /// <inheritdoc />
    public partial class AddFeeDuesClassAndOpeningBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "FeeStructureId",
                schema: "fee",
                table: "FeePayments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "ClassId",
                schema: "fee",
                table: "FeePayments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "fee",
                table: "FeePayments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_ClassId",
                schema: "fee",
                table: "FeePayments",
                column: "ClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeePayments_ClassId",
                schema: "fee",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "ClassId",
                schema: "fee",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "fee",
                table: "FeePayments");

            migrationBuilder.AlterColumn<Guid>(
                name: "FeeStructureId",
                schema: "fee",
                table: "FeePayments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}

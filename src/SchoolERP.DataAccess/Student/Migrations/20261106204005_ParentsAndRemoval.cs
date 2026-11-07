using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.DataAccess.Student.Migrations
{
    /// <inheritdoc />
    public partial class ParentsAndRemoval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Students",
                type: "varchar(255)",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "LinkedUserId",
                table: "Students",
                type: "char(36)",
                nullable: true,
                collation: "ascii_bin",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldCollation: "ascii_bin");

            migrationBuilder.AddColumn<string>(
                name: "FatherEmail",
                table: "Students",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                table: "Students",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "FatherPhone",
                table: "Students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MotherEmail",
                table: "Students",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MotherName",
                table: "Students",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "MotherPhone",
                table: "Students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "RemovalReason",
                table: "Students",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.AddColumn<DateTime>(
                name: "RemovedAtUtc",
                table: "Students",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Status",
                table: "Students",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_Status",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FatherEmail",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FatherName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "FatherPhone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MotherEmail",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MotherName",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "MotherPhone",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RemovalReason",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "RemovedAtUtc",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Students",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(255)")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<Guid>(
                name: "LinkedUserId",
                table: "Students",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_bin",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true,
                oldCollation: "ascii_bin");
        }
    }
}

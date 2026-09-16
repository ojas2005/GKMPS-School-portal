using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Student.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddParentUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentUserId",
                table: "Students",
                type: "char(36)",
                nullable: true,
                collation: "ascii_bin");

            migrationBuilder.CreateIndex(
                name: "IX_Students_ParentUserId",
                table: "Students",
                column: "ParentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_ParentUserId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "ParentUserId",
                table: "Students");
        }
    }
}

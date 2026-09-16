using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Identity.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGoogleAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_GoogleSubjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GoogleSubjectId",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleSubjectId",
                table: "Users",
                type: "varchar(255)",
                nullable: true,
                collation: "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Users_GoogleSubjectId",
                table: "Users",
                column: "GoogleSubjectId");
        }
    }
}

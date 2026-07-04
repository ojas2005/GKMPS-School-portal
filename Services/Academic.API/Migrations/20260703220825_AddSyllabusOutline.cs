using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Academic.Migrations
{
    /// <inheritdoc />
    public partial class AddSyllabusOutline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SyllabusOutline",
                schema: "academic",
                table: "Subjects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SyllabusOutline",
                schema: "academic",
                table: "Subjects");
        }
    }
}

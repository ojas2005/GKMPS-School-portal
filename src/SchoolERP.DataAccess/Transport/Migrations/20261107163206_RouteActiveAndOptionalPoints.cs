using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.DataAccess.Transport.Migrations
{
    /// <inheritdoc />
    public partial class RouteActiveAndOptionalPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StartPoint",
                table: "Routes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Routes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "EndPoint",
                table: "Routes",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "longtext")
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Routes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Routes");

            migrationBuilder.UpdateData(
                table: "Routes",
                keyColumn: "StartPoint",
                keyValue: null,
                column: "StartPoint",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "StartPoint",
                table: "Routes",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Routes",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.UpdateData(
                table: "Routes",
                keyColumn: "EndPoint",
                keyValue: null,
                column: "EndPoint",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "EndPoint",
                table: "Routes",
                type: "longtext",
                nullable: false,
                collation: "utf8mb4_general_ci",
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "utf8mb4_general_ci");
        }
    }
}

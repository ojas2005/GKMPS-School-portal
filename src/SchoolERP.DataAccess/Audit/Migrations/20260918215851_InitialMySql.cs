using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.DataAccess.Audit.Migrations
{
    /// <inheritdoc />
    public partial class InitialMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Action = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false, collation: "utf8mb4_general_ci"),
                    ActorUserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_bin"),
                    ActorRole = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true, collation: "utf8mb4_general_ci"),
                    EntityId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_general_ci"),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true, collation: "utf8mb4_general_ci"),
                    Detail = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_general_ci")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActorUserId_TimestampUtc",
                table: "AuditEntries",
                columns: new[] { "ActorUserId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityId_TimestampUtc",
                table: "AuditEntries",
                columns: new[] { "EntityId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TimestampUtc",
                table: "AuditEntries",
                column: "TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}

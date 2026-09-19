using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.DataAccess.Transport.Migrations
{
    /// <inheritdoc />
    public partial class InitialMySql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    ActorUserId = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    ActorRole = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    Action = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    EntityName = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    EntityId = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    BeforeStateJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    AfterStateJson = table.Column<string>(type: "longtext", nullable: true, collation: "utf8mb4_general_ci"),
                    TimestampUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    Name = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    StartPoint = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    EndPoint = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    MonthlyFee = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "StudentRouteMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    StudentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    RouteId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    PickupPoint = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    AssignedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRouteMappings", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_bin"),
                    RegistrationNumber = table.Column<string>(type: "varchar(255)", nullable: false, collation: "utf8mb4_general_ci"),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    DriverName = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    DriverPhone = table.Column<string>(type: "longtext", nullable: false, collation: "utf8mb4_general_ci"),
                    RouteId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_bin"),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                })
                .Annotation("Relational:Collation", "utf8mb4_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRouteMappings_RouteId",
                table: "StudentRouteMappings",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRouteMappings_StudentId",
                table: "StudentRouteMappings",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNumber",
                table: "Vehicles",
                column: "RegistrationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "StudentRouteMappings");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}

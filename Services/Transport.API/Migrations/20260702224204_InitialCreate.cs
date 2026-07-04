using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolERP.Transport.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "transport");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "transport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    BeforeStateJson = table.Column<string>(type: "text", nullable: true),
                    AfterStateJson = table.Column<string>(type: "text", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                schema: "transport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartPoint = table.Column<string>(type: "text", nullable: false),
                    EndPoint = table.Column<string>(type: "text", nullable: false),
                    MonthlyFee = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentRouteMappings",
                schema: "transport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PickupPoint = table.Column<string>(type: "text", nullable: false),
                    AssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentRouteMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                schema: "transport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    DriverName = table.Column<string>(type: "text", nullable: false),
                    DriverPhone = table.Column<string>(type: "text", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "transport",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRouteMappings_RouteId",
                schema: "transport",
                table: "StudentRouteMappings",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRouteMappings_StudentId",
                schema: "transport",
                table: "StudentRouteMappings",
                column: "StudentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_RegistrationNumber",
                schema: "transport",
                table: "Vehicles",
                column: "RegistrationNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "transport");

            migrationBuilder.DropTable(
                name: "Routes",
                schema: "transport");

            migrationBuilder.DropTable(
                name: "StudentRouteMappings",
                schema: "transport");

            migrationBuilder.DropTable(
                name: "Vehicles",
                schema: "transport");
        }
    }
}

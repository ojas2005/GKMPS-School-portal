using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SchoolERP.Fee.Data;

#nullable disable

namespace SchoolERP.Fee.Migrations
{
    /// <summary>
    /// Hand-authored to match Data/FeeDbContext.cs. Note FeePayment.Status is a computed
    /// C# property (Ignore()'d in the DbContext) and has no column here. The
    /// InboxState/OutboxMessage/OutboxState tables below are copied verbatim (column-for-
    /// column) from Identity.API's real, SDK-generated migration -- the one part of this
    /// environment that did produce authoritative `dotnet ef` output -- rather than
    /// re-guessed by hand, since MassTransit's EF outbox schema is otherwise easy to get
    /// subtly wrong. See the note in Identity.API's InitialCreate migration and README
    /// "Migrations" for the rest of this file's provenance.
    /// </summary>
    [DbContext(typeof(FeeDbContext))]
    [Migration("20260702000001_InitialCreate")]
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "fee");

            migrationBuilder.CreateTable(
                name: "InboxState",
                schema: "fee",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Received = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiveCount = table.Column<int>(type: "integer", nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Consumed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxState", x => x.Id);
                    table.UniqueConstraint("AK_InboxState_MessageId_ConsumerId", x => new { x.MessageId, x.ConsumerId });
                });

            migrationBuilder.CreateTable(
                name: "OutboxState",
                schema: "fee",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_OutboxState", x => x.OutboxId));

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "fee",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnqueueTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Headers = table.Column<string>(type: "text", nullable: true),
                    Properties = table.Column<string>(type: "text", nullable: true),
                    InboxMessageId = table.Column<Guid>(type: "uuid", nullable: true),
                    InboxConsumerId = table.Column<Guid>(type: "uuid", nullable: true),
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: true),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    DestinationAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ResponseAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    FaultAddress = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExpirationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessage", x => x.SequenceNumber);
                    table.ForeignKey(
                        name: "FK_OutboxMessage_InboxState_InboxMessageId_InboxConsumerId",
                        columns: x => new { x.InboxMessageId, x.InboxConsumerId },
                        principalSchema: "fee",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "fee",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateTable(
                name: "FeeStructures",
                schema: "fee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    AcademicYear = table.Column<string>(type: "text", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_FeeStructures", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FeePayments",
                schema: "fee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FeeStructureId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    IsWaiverRequested = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsWaiverApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    WaiverAmount = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 0m),
                    WaiverApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeePayments_FeeStructures_FeeStructureId",
                        column: x => x.FeeStructureId,
                        principalSchema: "fee",
                        principalTable: "FeeStructures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                schema: "fee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FeePaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    GatewayReference = table.Column<string>(type: "text", nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceiptBlobPath = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_PaymentTransactions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "fee",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ActorUserId = table.Column<string>(type: "text", nullable: false),
                    ActorRole = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    BeforeStateJson = table.Column<string>(type: "text", nullable: true),
                    AfterStateJson = table.Column<string>(type: "text", nullable: true),
                    TimestampUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

            migrationBuilder.CreateIndex(name: "IX_FeeStructures_ClassId_AcademicYear", schema: "fee", table: "FeeStructures", columns: new[] { "ClassId", "AcademicYear" });
            migrationBuilder.CreateIndex(name: "IX_FeePayments_StudentId_FeeStructureId", schema: "fee", table: "FeePayments", columns: new[] { "StudentId", "FeeStructureId" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_FeePayments_FeeStructureId", schema: "fee", table: "FeePayments", column: "FeeStructureId");
            migrationBuilder.CreateIndex(name: "IX_PaymentTransactions_ReceiptNumber", schema: "fee", table: "PaymentTransactions", column: "ReceiptNumber", unique: true);
            migrationBuilder.CreateIndex(name: "IX_PaymentTransactions_FeePaymentId", schema: "fee", table: "PaymentTransactions", column: "FeePaymentId");
            migrationBuilder.CreateIndex(name: "IX_AuditLogs_EntityName_EntityId", schema: "fee", table: "AuditLogs", columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(name: "IX_InboxState_Delivered", schema: "fee", table: "InboxState", column: "Delivered");
            migrationBuilder.CreateIndex(name: "IX_OutboxMessage_EnqueueTime", schema: "fee", table: "OutboxMessage", column: "EnqueueTime");
            migrationBuilder.CreateIndex(name: "IX_OutboxMessage_ExpirationTime", schema: "fee", table: "OutboxMessage", column: "ExpirationTime");
            migrationBuilder.CreateIndex(name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber", schema: "fee", table: "OutboxMessage", columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_OutboxMessage_OutboxId_SequenceNumber", schema: "fee", table: "OutboxMessage", columns: new[] { "OutboxId", "SequenceNumber" }, unique: true);
            migrationBuilder.CreateIndex(name: "IX_OutboxState_Created", schema: "fee", table: "OutboxState", column: "Created");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PaymentTransactions", schema: "fee");
            migrationBuilder.DropTable(name: "OutboxMessage", schema: "fee");
            migrationBuilder.DropTable(name: "FeePayments", schema: "fee");
            migrationBuilder.DropTable(name: "FeeStructures", schema: "fee");
            migrationBuilder.DropTable(name: "AuditLogs", schema: "fee");
            migrationBuilder.DropTable(name: "InboxState", schema: "fee");
            migrationBuilder.DropTable(name: "OutboxState", schema: "fee");
        }
    }
}

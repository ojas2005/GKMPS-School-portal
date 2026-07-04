using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SchoolERP.Student.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "student");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "student",
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
                name: "InboxState",
                schema: "student",
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
                schema: "student",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    LockId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Delivered = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxState", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdmissionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: false),
                    ClassId = table.Column<string>(type: "text", nullable: false),
                    SectionId = table.Column<string>(type: "text", nullable: false),
                    AdmissionDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ParentName = table.Column<string>(type: "text", nullable: true),
                    ParentEmail = table.Column<string>(type: "text", nullable: true),
                    ParentPhone = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TransferredOutAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessage",
                schema: "student",
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
                        principalSchema: "student",
                        principalTable: "InboxState",
                        principalColumns: new[] { "MessageId", "ConsumerId" });
                    table.ForeignKey(
                        name: "FK_OutboxMessage_OutboxState_OutboxId",
                        column: x => x.OutboxId,
                        principalSchema: "student",
                        principalTable: "OutboxState",
                        principalColumn: "OutboxId");
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    BlobPath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransferCertificates",
                schema: "student",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    RequestedLeavingDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSubmitted = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BlobPath = table.Column<string>(type: "text", nullable: true),
                    IsPdfGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransferCertificates_Students_StudentId",
                        column: x => x.StudentId,
                        principalSchema: "student",
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityName_EntityId",
                schema: "student",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_StudentId_DocumentType",
                schema: "student",
                table: "Documents",
                columns: new[] { "StudentId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "IX_InboxState_Delivered",
                schema: "student",
                table: "InboxState",
                column: "Delivered");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_EnqueueTime",
                schema: "student",
                table: "OutboxMessage",
                column: "EnqueueTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_ExpirationTime",
                schema: "student",
                table: "OutboxMessage",
                column: "ExpirationTime");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber",
                schema: "student",
                table: "OutboxMessage",
                columns: new[] { "InboxMessageId", "InboxConsumerId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessage_OutboxId_SequenceNumber",
                schema: "student",
                table: "OutboxMessage",
                columns: new[] { "OutboxId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxState_Created",
                schema: "student",
                table: "OutboxState",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_Students_AdmissionNumber",
                schema: "student",
                table: "Students",
                column: "AdmissionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_ClassId_SectionId",
                schema: "student",
                table: "Students",
                columns: new[] { "ClassId", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TransferCertificates_StudentId",
                schema: "student",
                table: "TransferCertificates",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferCertificates_VerificationCode",
                schema: "student",
                table: "TransferCertificates",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Documents",
                schema: "student");

            migrationBuilder.DropTable(
                name: "OutboxMessage",
                schema: "student");

            migrationBuilder.DropTable(
                name: "TransferCertificates",
                schema: "student");

            migrationBuilder.DropTable(
                name: "InboxState",
                schema: "student");

            migrationBuilder.DropTable(
                name: "OutboxState",
                schema: "student");

            migrationBuilder.DropTable(
                name: "Students",
                schema: "student");
        }
    }
}

-- Fee.API schema. Matches Services/Fee.API/Data/FeeDbContext.cs.
-- Note: FeePayment.Status is a computed C# property (Ignore()'d in EF config) and has
-- no column here -- it's derived at read time from PaidAmount/WaiverAmount/TotalAmount.

SET search_path TO fee;

CREATE TABLE IF NOT EXISTS "FeeStructures" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ClassId" text NOT NULL,
    "Name" text NOT NULL,
    "Amount" numeric NOT NULL,
    "AcademicYear" text NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_FeeStructures_ClassId_AcademicYear" ON "FeeStructures" ("ClassId", "AcademicYear");

CREATE TABLE IF NOT EXISTS "FeePayments" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "FeeStructureId" uuid NOT NULL,
    "TotalAmount" numeric NOT NULL,
    "PaidAmount" numeric NOT NULL DEFAULT 0,
    "IsWaiverRequested" boolean NOT NULL DEFAULT FALSE,
    "IsWaiverApproved" boolean NOT NULL DEFAULT FALSE,
    "WaiverAmount" numeric NOT NULL DEFAULT 0,
    "WaiverApprovedByUserId" uuid NULL,
    CONSTRAINT "FK_FeePayments_FeeStructures_FeeStructureId" FOREIGN KEY ("FeeStructureId") REFERENCES "FeeStructures" ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_FeePayments_StudentId_FeeStructureId" ON "FeePayments" ("StudentId", "FeeStructureId");

CREATE TABLE IF NOT EXISTS "PaymentTransactions" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "FeePaymentId" uuid NOT NULL,
    "Amount" numeric NOT NULL,
    "ReceiptNumber" varchar(64) NOT NULL,
    "PaymentMethod" text NOT NULL,
    "GatewayReference" text NULL,
    "PaidAtUtc" timestamp with time zone NOT NULL,
    "ReceiptBlobPath" text NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentTransactions_ReceiptNumber" ON "PaymentTransactions" ("ReceiptNumber");
CREATE INDEX IF NOT EXISTS "IX_PaymentTransactions_FeePaymentId" ON "PaymentTransactions" ("FeePaymentId");

CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ActorUserId" text NOT NULL,
    "ActorRole" text NOT NULL,
    "Action" text NOT NULL,
    "EntityName" text NOT NULL,
    "EntityId" text NOT NULL,
    "BeforeStateJson" text NULL,
    "AfterStateJson" text NULL,
    "TimestampUtc" timestamp with time zone NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_EntityName_EntityId" ON "AuditLogs" ("EntityName", "EntityId");

-- Reporting.API schema. Matches Services/Reporting.API/Data/ReportingDbContext.cs.

SET search_path TO reporting;

CREATE TABLE IF NOT EXISTS "ReportSnapshots" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ReportType" text NOT NULL,
    "FromUtc" timestamp with time zone NULL,
    "ToUtc" timestamp with time zone NULL,
    "ResultJson" jsonb NOT NULL,
    "GeneratedByUserId" uuid NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_ReportSnapshots_ReportType_CreatedAtUtc" ON "ReportSnapshots" ("ReportType", "CreatedAtUtc");

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

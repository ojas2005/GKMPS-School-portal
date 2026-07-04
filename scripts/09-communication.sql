-- Communication.API schema. Matches Services/Communication.API/Data/CommunicationDbContext.cs.

SET search_path TO communication;

CREATE TABLE IF NOT EXISTS "Announcements" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Title" text NOT NULL,
    "Body" text NOT NULL,
    "TargetRolesCsv" text NULL,
    "TargetClassId" text NULL,
    "PostedByUserId" uuid NOT NULL,
    "PublishedAtUtc" timestamp with time zone NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_Announcements_PublishedAtUtc" ON "Announcements" ("PublishedAtUtc");

CREATE TABLE IF NOT EXISTS "ParentMessages" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "SenderUserId" uuid NOT NULL,
    "RecipientUserId" uuid NOT NULL,
    "Body" text NOT NULL,
    "IsRead" boolean NOT NULL DEFAULT FALSE,
    "ReadAtUtc" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_ParentMessages_StudentId_CreatedAtUtc" ON "ParentMessages" ("StudentId", "CreatedAtUtc");
CREATE INDEX IF NOT EXISTS "IX_ParentMessages_RecipientUserId_IsRead" ON "ParentMessages" ("RecipientUserId", "IsRead");

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

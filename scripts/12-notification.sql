-- Notification.API schema. Matches Services/Notification.API/Data/NotificationDbContext.cs.

SET search_path TO notification;

CREATE TABLE IF NOT EXISTS "NotificationLogs" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "EventType" text NOT NULL,
    "RecipientReference" text NOT NULL,
    "DispatchChannel" text NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "IsDelivered" boolean NOT NULL DEFAULT FALSE,
    "DeliveredAtUtc" timestamp with time zone NULL,
    "FailureReason" text NULL
);
CREATE INDEX IF NOT EXISTS "IX_NotificationLogs_EventType_CreatedAtUtc" ON "NotificationLogs" ("EventType", "CreatedAtUtc");

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

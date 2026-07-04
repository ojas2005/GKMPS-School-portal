-- Transport.API schema. Matches Services/Transport.API/Data/TransportDbContext.cs.

SET search_path TO transport;

CREATE TABLE IF NOT EXISTS "Routes" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Name" text NOT NULL,
    "StartPoint" text NOT NULL,
    "EndPoint" text NOT NULL,
    "MonthlyFee" numeric NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS "Vehicles" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "RegistrationNumber" text NOT NULL,
    "Capacity" integer NOT NULL,
    "DriverName" text NOT NULL,
    "DriverPhone" text NOT NULL,
    "RouteId" uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Vehicles_RegistrationNumber" ON "Vehicles" ("RegistrationNumber");

CREATE TABLE IF NOT EXISTS "StudentRouteMappings" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "RouteId" uuid NOT NULL,
    "PickupPoint" text NOT NULL,
    "AssignedAtUtc" timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_StudentRouteMappings_StudentId" ON "StudentRouteMappings" ("StudentId");
CREATE INDEX IF NOT EXISTS "IX_StudentRouteMappings_RouteId" ON "StudentRouteMappings" ("RouteId");

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

-- Identity.API schema. Hand-authored to exactly match Services/Identity.API/Data/IdentityDbContext.cs
-- and its entities (Entities/User.cs, Entities/RefreshToken.cs) plus SchoolERP.Shared's AuditLog.
--
-- This is a provisional stand-in for `dotnet ef migrations add InitialCreate` (this build
-- environment has no .NET SDK available to run that command for real). Once you have the
-- SDK, generate the real migration and this file becomes redundant -- see README.md.

SET search_path TO identity;

CREATE TABLE IF NOT EXISTS "Users" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Email" varchar(256) NOT NULL,
    "PasswordHash" text NULL,
    "FullName" varchar(200) NOT NULL,
    "Role" varchar(50) NOT NULL,
    "IsEmailVerified" boolean NOT NULL DEFAULT FALSE,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "GoogleSubjectId" text NULL,
    "LastLoginAtUtc" timestamp with time zone NULL,
    "LinkedProfileId" uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_GoogleSubjectId" ON "Users" ("GoogleSubjectId");

CREATE TABLE IF NOT EXISTS "RefreshTokens" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "UserId" uuid NOT NULL,
    "TokenHash" varchar(512) NOT NULL,
    "ExpiresAtUtc" timestamp with time zone NOT NULL,
    "RevokedAtUtc" timestamp with time zone NULL,
    "ReplacedByTokenHash" text NULL,
    "CreatedByIp" text NULL,
    CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_RefreshTokens_TokenHash" ON "RefreshTokens" ("TokenHash");
CREATE INDEX IF NOT EXISTS "IX_RefreshTokens_UserId_ExpiresAtUtc" ON "RefreshTokens" ("UserId", "ExpiresAtUtc");

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
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_TimestampUtc" ON "AuditLogs" ("TimestampUtc");

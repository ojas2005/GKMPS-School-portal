-- Staff.API schema. Matches Services/Staff.API/Data/StaffDbContext.cs. See 02-identity.sql
-- header for the "why hand-authored SQL instead of an EF migration" explanation.

SET search_path TO staff;

CREATE TABLE IF NOT EXISTS "Staff" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "LinkedUserId" uuid NOT NULL,
    "EmployeeCode" varchar(50) NOT NULL,
    "FullName" varchar(200) NOT NULL,
    "Designation" text NOT NULL,
    "SubjectsTaughtCsv" text NULL,
    "DateOfJoiningUtc" timestamp with time zone NOT NULL,
    "Phone" text NULL,
    "Email" text NULL,
    "Status" text NOT NULL DEFAULT 'Active'
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Staff_EmployeeCode" ON "Staff" ("EmployeeCode");

CREATE TABLE IF NOT EXISTS "LeaveRequests" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StaffId" uuid NOT NULL,
    "LeaveType" text NOT NULL,
    "FromDateUtc" timestamp with time zone NOT NULL,
    "ToDateUtc" timestamp with time zone NOT NULL,
    "Reason" text NOT NULL,
    "IsSubmitted" boolean NOT NULL DEFAULT FALSE,
    "SubmittedAtUtc" timestamp with time zone NULL,
    "IsApproved" boolean NOT NULL DEFAULT FALSE,
    "IsRejected" boolean NOT NULL DEFAULT FALSE,
    "DecidedAtUtc" timestamp with time zone NULL,
    "DecidedByUserId" uuid NULL,
    "DecisionNote" text NULL,
    CONSTRAINT "FK_LeaveRequests_Staff_StaffId" FOREIGN KEY ("StaffId") REFERENCES "Staff" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_LeaveRequests_StaffId_FromDateUtc" ON "LeaveRequests" ("StaffId", "FromDateUtc");

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

-- Attendance.API schema. Matches Services/Attendance.API/Data/AttendanceDbContext.cs.

SET search_path TO attendance;

CREATE TABLE IF NOT EXISTS "AttendanceRecords" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "ClassId" text NOT NULL,
    "SectionId" text NOT NULL,
    "Date" date NOT NULL,
    "Status" text NOT NULL DEFAULT 'Present',
    "ArrivalTime" time NULL,
    "IsLate" boolean NOT NULL DEFAULT FALSE,
    "MarkedByUserId" uuid NOT NULL,
    "MarkedAtUtc" timestamp with time zone NOT NULL
);
-- Data-integrity guarantee: one attendance record per student per day.
CREATE UNIQUE INDEX IF NOT EXISTS "IX_AttendanceRecords_StudentId_Date" ON "AttendanceRecords" ("StudentId", "Date");
CREATE INDEX IF NOT EXISTS "IX_AttendanceRecords_Class_Date" ON "AttendanceRecords" ("ClassId", "SectionId", "Date");

CREATE TABLE IF NOT EXISTS "MonthlySummaries" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "Year" integer NOT NULL,
    "Month" integer NOT NULL,
    "PresentCount" integer NOT NULL DEFAULT 0,
    "AbsentCount" integer NOT NULL DEFAULT 0,
    "LateCount" integer NOT NULL DEFAULT 0,
    "TotalMarkedDays" integer NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_MonthlySummaries_Student_Year_Month" ON "MonthlySummaries" ("StudentId", "Year", "Month");

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

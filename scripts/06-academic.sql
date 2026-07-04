-- Academic.API schema. Matches Services/Academic.API/Data/AcademicDbContext.cs.

SET search_path TO academic;

CREATE TABLE IF NOT EXISTS "Subjects" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Code" text NOT NULL,
    "Name" text NOT NULL,
    "ClassId" text NOT NULL,
    "TeacherStaffId" uuid NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Subjects_ClassId_Code" ON "Subjects" ("ClassId", "Code");

CREATE TABLE IF NOT EXISTS "Timetables" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ClassId" text NOT NULL,
    "SectionId" text NOT NULL,
    "SlotsJson" jsonb NOT NULL,
    "EffectiveFromUtc" timestamp with time zone NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Timetables_ClassId_SectionId" ON "Timetables" ("ClassId", "SectionId");

CREATE TABLE IF NOT EXISTS "Homeworks" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ClassId" text NOT NULL,
    "SectionId" text NOT NULL,
    "SubjectId" uuid NOT NULL,
    "Title" text NOT NULL,
    "Description" text NULL,
    "AssignedDateUtc" timestamp with time zone NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL,
    "AssignedByStaffId" uuid NOT NULL
);
CREATE INDEX IF NOT EXISTS "IX_Homeworks_Class_Due" ON "Homeworks" ("ClassId", "SectionId", "DueDateUtc");

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

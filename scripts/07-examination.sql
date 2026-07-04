-- Examination.API schema. Matches Services/Examination.API/Data/ExaminationDbContext.cs.

SET search_path TO examination;

CREATE TABLE IF NOT EXISTS "Exams" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Name" text NOT NULL,
    "ClassId" text NOT NULL,
    "SubjectId" uuid NOT NULL,
    "ExamDateUtc" timestamp with time zone NOT NULL,
    "MaxMarks" integer NOT NULL,
    "PassingMarks" integer NOT NULL,
    "AnswerKeyJson" jsonb NULL,
    "IsResultPublished" boolean NOT NULL DEFAULT FALSE,
    "ResultPublishedAtUtc" timestamp with time zone NULL
);
CREATE INDEX IF NOT EXISTS "IX_Exams_Class_Subject_Date" ON "Exams" ("ClassId", "SubjectId", "ExamDateUtc");

CREATE TABLE IF NOT EXISTS "MarksEntries" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "ExamId" uuid NOT NULL,
    "StudentId" uuid NOT NULL,
    "MarksObtained" numeric NOT NULL,
    "Grade" text NULL,
    "Remarks" text NULL,
    "EnteredByStaffId" uuid NOT NULL,
    CONSTRAINT "FK_MarksEntries_Exams_ExamId" FOREIGN KEY ("ExamId") REFERENCES "Exams" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_MarksEntries_ExamId_StudentId" ON "MarksEntries" ("ExamId", "StudentId");

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

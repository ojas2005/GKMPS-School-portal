-- Student.API schema. Hand-authored to match Services/Student.API/Data/StudentDbContext.cs
-- and its entities. Provisional stand-in for a real EF Core migration -- see README.md.

SET search_path TO student;

CREATE TABLE IF NOT EXISTS "Students" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "LinkedUserId" uuid NOT NULL,
    "AdmissionNumber" varchar(50) NOT NULL,
    "FullName" varchar(200) NOT NULL,
    "DateOfBirth" timestamp with time zone NOT NULL,
    "Gender" text NOT NULL,
    "ClassId" text NOT NULL,
    "SectionId" text NOT NULL,
    "AdmissionDateUtc" timestamp with time zone NOT NULL,
    "ParentName" text NULL,
    "ParentEmail" text NULL,
    "ParentPhone" text NULL,
    "Address" text NULL,
    "Status" text NOT NULL DEFAULT 'Active',
    "TransferredOutAtUtc" timestamp with time zone NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_AdmissionNumber" ON "Students" ("AdmissionNumber");
CREATE INDEX IF NOT EXISTS "IX_Students_ClassId_SectionId" ON "Students" ("ClassId", "SectionId");

CREATE TABLE IF NOT EXISTS "Documents" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "DocumentType" text NOT NULL,
    "BlobPath" text NOT NULL,
    "ContentType" text NOT NULL,
    "SizeBytes" bigint NOT NULL DEFAULT 0,
    "UploadedAtUtc" timestamp with time zone NOT NULL,
    CONSTRAINT "FK_Documents_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_Documents_StudentId_DocumentType" ON "Documents" ("StudentId", "DocumentType");

CREATE TABLE IF NOT EXISTS "TransferCertificates" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "StudentId" uuid NOT NULL,
    "Reason" text NOT NULL,
    "RequestedLeavingDateUtc" timestamp with time zone NOT NULL,
    "IsSubmitted" boolean NOT NULL DEFAULT FALSE,
    "SubmittedAtUtc" timestamp with time zone NULL,
    "SubmittedByUserId" uuid NULL,
    "IsApproved" boolean NOT NULL DEFAULT FALSE,
    "ApprovedAtUtc" timestamp with time zone NULL,
    "ApprovedByUserId" uuid NULL,
    "VerificationCode" varchar(64) NOT NULL,
    "BlobPath" text NULL,
    "IsPdfGenerated" boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_TransferCertificates_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("Id") ON DELETE CASCADE
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_TransferCertificates_VerificationCode" ON "TransferCertificates" ("VerificationCode");
CREATE INDEX IF NOT EXISTS "IX_TransferCertificates_StudentId" ON "TransferCertificates" ("StudentId");

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

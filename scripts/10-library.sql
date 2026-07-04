-- Library.API schema. Matches Services/Library.API/Data/LibraryDbContext.cs.
-- Note: BookIssue.IsReturned is a computed C# property (Ignore()'d in EF config) --
-- derived from ReturnedAtUtc, not a stored column.

SET search_path TO library;

CREATE TABLE IF NOT EXISTS "Books" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "Isbn" varchar(20) NOT NULL,
    "Title" text NOT NULL,
    "Author" text NOT NULL,
    "Category" text NOT NULL,
    "TotalCopies" integer NOT NULL DEFAULT 0,
    "AvailableCopies" integer NOT NULL DEFAULT 0
);
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Books_Isbn" ON "Books" ("Isbn");

CREATE TABLE IF NOT EXISTS "BookIssues" (
    "Id" uuid PRIMARY KEY,
    "CreatedAtUtc" timestamp with time zone NOT NULL,
    "UpdatedAtUtc" timestamp with time zone NULL,
    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
    "BookId" uuid NOT NULL,
    "StudentId" uuid NOT NULL,
    "IssuedAtUtc" timestamp with time zone NOT NULL,
    "DueDateUtc" timestamp with time zone NOT NULL,
    "ReturnedAtUtc" timestamp with time zone NULL,
    "FineAmount" numeric NOT NULL DEFAULT 0,
    "IsFinePaid" boolean NOT NULL DEFAULT FALSE,
    CONSTRAINT "FK_BookIssues_Books_BookId" FOREIGN KEY ("BookId") REFERENCES "Books" ("Id") ON DELETE RESTRICT
);
CREATE INDEX IF NOT EXISTS "IX_BookIssues_Student_Book_Returned" ON "BookIssues" ("StudentId", "BookId", "ReturnedAtUtc");

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

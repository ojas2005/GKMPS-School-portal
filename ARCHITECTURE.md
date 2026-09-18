# GKMPS School ERP — Architecture Diagrams

HLD · LLD · per-module ER · one ASP.NET Core 9 app in 4 tiers (Api → Business → DataAccess, + Common) · TiDB · Caddy

> Diagrams are Mermaid source, rendered natively by GitHub — no external tools needed to view them.

## Contents

**HLD**
- [1. System architecture](#1-system-architecture)
- [2. Events and cross-module calls](#2-events-and-cross-module-calls)

**LLD**
- [3. N-tier layering](#3-n-tier-layering)
- [4. Auth: login, lockout, refresh rotation](#4-auth-login-lockout-refresh-rotation-self-service-scoping)
- [5. The atomic-update pattern](#5-the-atomic-update-pattern-used-in-4-modules)

**ER** — [Identity](#identity-module--er) · [Student](#student-module--er) · [Staff](#staff-module--er) · [Attendance](#attendance-module--er) · [Academic](#academic-module--er) · [Examination](#examination-module--er) · [Fee](#fee-module--er) · [Communication](#communication-module--er) · [Library](#library-module--er) · [Transport](#transport-module--er) · [Notification](#notification-module--er) · [Reporting](#reporting-module--er)

---

## 1. System architecture

A single deployable ASP.NET Core app (`SchoolERP.Api`) behind Caddy. It is organised into tiers, and within each tier into the 12 school modules (Identity, Student, Staff, Attendance, Academic, Examination, Fee, Communication, Library, Transport, Notification, Reporting). Each module keeps its **own database** on one TiDB (MySQL-protocol) server. Receipts, report cards and transfer certificates are PDFs kept in their own `files` database, handed out as short-lived signed links that the API serves itself (Azure Blob Storage with SAS links remains an opt-in alternative).

```mermaid
flowchart TB
  SPA["Angular SPA (Static Web Apps / ng serve)"]:::client
  CAD["Caddy (TLS termination, HSTS)"]:::edge
  subgraph APP["SchoolERP.Api — one process"]
    direction TB
    API["Presentation tier<br/>controllers · JWT · CORS · rate limiting · health"]:::tier
    BIZ["Business tier<br/>services · DTOs · token issuing · PDFs · event handlers"]:::tier
    DAL["Data access tier<br/>12 DbContexts + migrations · repositories · file store"]:::tier
    API --> BIZ --> DAL
  end
  DB[("TiDB server<br/>identity · student · staff · attendance · academic · examination<br/>fee · communication · library · transport · notification · reporting")]:::infra
  BLOB[("files database<br/>signed PDF links")]:::infra
  SMTP(["SMTP relay (optional)<br/>account / admission emails"]):::infra

  SPA -->|"HTTPS /api/*"| CAD --> API
  DAL --> DB
  DAL -. store / signed link .-> BLOB
  BIZ -. email .-> SMTP

  classDef client fill:#ede9fe,stroke:#7c3aed,color:#4c1d95;
  classDef edge fill:#d1fae5,stroke:#059669,color:#064e3b,font-weight:bold;
  classDef tier fill:#ffffff,stroke:#6ee7b7,color:#0f172a;
  classDef infra fill:#ecfeff,stroke:#0891b2,color:#155e75,font-weight:bold;
```

Why one app: the school has ~1,300 accounts and a few hundred daily users. The earlier 12-service design needed ~1.5 GB of RAM across 17 containers (services, gateway, RabbitMQ, Redis); this app uses ~200 MB, starts in seconds, and fits free or near-free hosting — while keeping the same module boundaries, databases and API routes.

## 2. Events and cross-module calls

Modules call each other's **business services** directly (in-process), never another module's tables. Side effects that shouldn't slow a request down go through an in-process event bus.

```mermaid
flowchart LR
  REG["Register account (Identity)"] -->|"UserRegisteredEvent"| BUS(["In-process event bus<br/>bounded queue + background dispatcher"])
  ADM["Admit student (Student)"] -->|"StudentEnrolledEvent"| BUS
  PAY["Record payment (Fee)"] -->|"FeePaidEvent"| BUS
  CERT["Generate transfer certificate (Student)"] -->|"CertificateGeneratedEvent"| BUS
  BUS --> NH["Notification handlers<br/>log attempt · send email (SMTP) · record delivered/failed"]

  RPT["Reporting: enrollment / fee collection"] -->|"IStudentService"| ST2["Student module"]
  RPT -->|"IFeePaymentService"| FE2["Fee module"]
  TRN["Transport: route roster"] -->|"IStudentService"| ST2
  LOGIN["Identity: login claims"] -->|"student / staff repositories"| PROF["Student & Staff profiles"]

  classDef e fill:#ffffff,stroke:#94a3b8,color:#0f172a;
  class REG,ADM,PAY,CERT,NH,RPT,ST2,FE2,TRN,LOGIN,PROF e;
```

Events are raised **after** the change is saved, and handlers run on a background worker in their own DI scope, so a slow email never delays the request. Queued events live in memory: one raised in the instant before the process stops is lost, which is acceptable because events only drive notifications (every attempt is also recorded in `NotificationLogs`). `CertificateGeneratedEvent` carries a verification code, never a download link.

## 3. N-tier layering

```mermaid
flowchart TB
  subgraph P["SchoolERP.Api — presentation"]
    C["Controllers/{Module}<br/>attribute routes · [Authorize(Roles)] · caller-scope checks"]
    PR["Program.cs · Startup/DatabaseInitializer<br/>JWT · CORS · rate limiter · Swagger · health · migrations · owner seed"]
  end
  subgraph B["SchoolERP.Business — business rules"]
    SI["{Module}/Services/Interfaces"] --> S["{Module}/Services<br/>rules · atomic updates · events"]
    D["{Module}/DTOs"]
    X["Identity/Auth (tokens, profile resolvers) · {Module}/Documents (QuestPDF) · Notification/Handlers · Common/Events"]
  end
  subgraph DA["SchoolERP.DataAccess — persistence"]
    RI["{Module}/Repositories/Interfaces"] --> R["{Module}/Repositories (EF Core)"]
    CTX["{Module}/{Module}DbContext + Migrations"]
    EN["{Module}/Entities"]
    ST["Storage (files database)"]
  end
  CM["SchoolERP.Common — ApiResponse · RoleNames · CallerClaims · BaseEntity/AuditLog · event contracts · exception handling · logging · security headers · hosting"]

  C --> SI
  S --> RI
  R --> CTX --> EN
  P -.-> CM
  B -.-> CM
  DA -.-> CM
  classDef n fill:#ecfdf5,stroke:#059669,color:#064e3b;
  class C,PR,SI,S,D,X,RI,R,CTX,EN,ST,CM n;
```

Project references enforce the direction: **Api → Business → DataAccess → Common**. Controllers never touch repositories or DbContexts, and repositories never see DTOs or services.

`BaseEntity.IsDeleted` gives every entity soft delete — global query filters exclude it by default. Admin actions write structured `AUDIT` log lines (who/what/before/after) via Serilog.

## 4. Auth: login, lockout, refresh rotation, self-service scoping

JWT (15 min) + rotating hashed refresh tokens (7 days), PBKDF2 password hashing, account lockout, and role-based scope claims embedded at issue time.

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant API as SchoolERP.Api (AuthController)
  participant ID as AuthService (Business)
  participant DB as identity DB (TiDB)
  participant PR as Student / Staff repositories
  U->>API: POST /api/auth/login loginId, password
  API->>ID: "auth" rate limit 60/min per client
  ID->>DB: find user by email/username
  alt account locked
    ID-->>U: 401 too many failed attempts
  else
    ID->>ID: PBKDF2 verify password
    alt wrong password
      ID->>DB: ExecuteUpdateAsync FailedLoginAttempts += 1
      alt attempts >= 5
        ID->>DB: set LockoutEndUtc = now + 15min
        ID-->>U: 401 locked
      else
        ID-->>U: 401 invalid credentials
      end
    else correct password
      ID->>DB: reset FailedLoginAttempts, set LastLoginAtUtc
      ID->>PR: resolve linked student / child / staff profile
      ID->>ID: mint JWT with role + scope claims
      ID->>DB: store hashed refresh token
      ID-->>U: 200 accessToken, refreshToken, scope claims
    end
  end
```

On refresh, presenting an already-revoked token revokes the whole token family for that user (theft-reuse detection) — see `AuthService.RefreshAsync`.

## 5. The atomic-update pattern (used in 4 modules)

Fee.PaidAmount, Attendance's monthly counters, Library.AvailableCopies, and Identity's FailedLoginAttempts are all updated the same way: a single `ExecuteUpdateAsync` SQL statement, never a load-then-save.

```mermaid
flowchart LR
  A["Naive: load entity, mutate in memory, SaveChanges"] -->|"race: two requests read the same value"| BAD["Lost update, last write wins"]
  B["This codebase: ExecuteUpdateAsync SetProperty counter, counter + delta"] --> GOOD["Single atomic SQL UPDATE, no read, no race"]
  classDef bad fill:#fef2f2,stroke:#f87171,color:#991b1b;
  classDef good fill:#ecfdf5,stroke:#22c55e,color:#166534;
  class A,BAD bad;
  class B,GOOD good;
```

Trade-off: because it bypasses the change tracker, a previously-loaded in-memory copy goes stale immediately — code that needs the fresh value re-reads it (see the comment in `AuthService.LoginAsync`).

---

## Identity module — ER

Single accounts table for all 8 roles; refresh tokens are stored hashed, never raw.

```mermaid
erDiagram
  User ||--o{ RefreshToken : has
  User {
    Guid Id PK
    string Email UK
    string Username UK
    string PasswordHash
    string FullName
    string Role
    bool IsEmailVerified
    bool IsActive
    datetime LastLoginAtUtc
    int FailedLoginAttempts
    datetime LockoutEndUtc
    Guid LinkedProfileId
    bool IsDeleted
  }
  RefreshToken {
    Guid Id PK
    Guid UserId FK
    string TokenHash
    datetime ExpiresAtUtc
    datetime RevokedAtUtc
    string ReplacedByTokenHash
    string CreatedByIp
  }
```

## Student module — ER

Admission record, uploaded/generated documents, and the transfer-certificate two-step workflow with a public verification code.

```mermaid
erDiagram
  StudentProfile ||--o{ StudentDocument : has
  StudentProfile ||--o{ TransferCertificate : may_request
  StudentProfile {
    Guid Id PK
    Guid LinkedUserId
    string AdmissionNumber UK
    string FullName
    date DateOfBirth
    string Gender
    string ClassId
    string SectionId
    datetime AdmissionDateUtc
    string ParentName
    string ParentEmail
    string ParentPhone
    string Address
    string Status
  }
  StudentDocument {
    Guid Id PK
    Guid StudentId FK
    string DocumentType
    string BlobPath
    string ContentType
    long SizeBytes
  }
  TransferCertificate {
    Guid Id PK
    Guid StudentId FK
    string Reason
    datetime RequestedLeavingDateUtc
    bool IsSubmitted
    bool IsApproved
    string VerificationCode UK
    string BlobPath
    bool IsPdfGenerated
  }
```

## Staff module — ER

Staff profiles with class-teacher assignment, an immutable payout ledger, daily attendance, and a two-step leave workflow.

```mermaid
erDiagram
  StaffProfile ||--o{ Payout : receives
  StaffProfile ||--o{ StaffAttendanceRecord : has
  StaffProfile ||--o{ LeaveRequest : files
  StaffProfile {
    Guid Id PK
    Guid LinkedUserId
    string EmployeeCode UK
    string FullName
    string Designation
    string SubjectsTaughtCsv
    datetime DateOfJoiningUtc
    string Status
    string ClassTeacherOfClassId
    string ClassTeacherOfSectionId
    decimal MonthlySalary
  }
  Payout {
    Guid Id PK
    Guid StaffId FK
    decimal Amount
    string PeriodLabel
    datetime PaidOnUtc
    string Method
    string Reference
    Guid RecordedByUserId
  }
  StaffAttendanceRecord {
    Guid Id PK
    Guid StaffId FK
    date Date
    string Status
    Guid MarkedByUserId
  }
  LeaveRequest {
    Guid Id PK
    Guid StaffId FK
    string LeaveType
    datetime FromDateUtc
    datetime ToDateUtc
    bool IsSubmitted
    bool IsApproved
    bool IsRejected
  }
```

## Attendance module — ER

Daily records plus a monthly rollup that's bumped atomically.

```mermaid
erDiagram
  AttendanceRecord {
    Guid Id PK
    Guid StudentId
    string ClassId
    string SectionId
    date Date
    string Status
    time ArrivalTime
    bool IsLate
    Guid MarkedByUserId
  }
  MonthlyAttendanceSummary {
    Guid Id PK
    Guid StudentId
    int Year
    int Month
    int PresentCount
    int AbsentCount
    int LateCount
    int TotalMarkedDays
  }
```

Unique index on (StudentId, Date) backs the duplicate-prevention check `HasMarkedAttendanceTodayAsync`.

## Academic module — ER

Subjects are relational; timetable slots and the timetable-generator config are stored as JSON blobs, cached in memory on read.

```mermaid
erDiagram
  Subject {
    Guid Id PK
    string Code
    string Name
    string ClassId
    Guid TeacherStaffId
    string SyllabusOutline
  }
  Homework {
    Guid Id PK
    string ClassId
    string SectionId
    Guid SubjectId
    string Title
    string Description
    datetime AssignedDateUtc
    datetime DueDateUtc
    Guid AssignedByStaffId
  }
  Timetable {
    Guid Id PK
    string ClassId
    string SectionId
    string SlotsJson
    datetime EffectiveFromUtc
  }
  ScheduleConfig {
    Guid Id PK
    string ConfigJson
  }
```

`SlotsJson` holds `[{day,period,subjectId,teacherStaffId,startTime,endTime}]`. Slot count/shape per class varies and is never queried column-by-column — deserialize-and-validate in the service layer beats a rigid one-row-per-slot schema.

## Examination module — ER

Exams (with a JSON answer key) and one marks row per student per exam, duplicate-guarded.

```mermaid
erDiagram
  Exam ||--o{ MarksEntry : has
  Exam {
    Guid Id PK
    string Name
    string ClassId
    Guid SubjectId
    datetime ExamDateUtc
    int MaxMarks
    int PassingMarks
    string AnswerKeyJson
    bool IsResultPublished
    datetime ResultPublishedAtUtc
  }
  MarksEntry {
    Guid Id PK
    Guid ExamId FK
    Guid StudentId
    decimal MarksObtained
    string Grade
    string Remarks
    Guid EnteredByStaffId
  }
```

Unique (ExamId, StudentId) index backs `HasMarksEnteredAsync`; corrections go through a separate atomic update path.

## Fee module — ER

The richest schema: class fee structures, per-student dues with a computed Status, an immutable transaction ledger, and a two-step waiver approval.

```mermaid
erDiagram
  FeeStructure ||--o{ FeePayment : assessed_as
  FeePayment ||--o{ PaymentTransaction : receipted_by
  FeeStructure {
    Guid Id PK
    string ClassId
    string Name
    decimal Amount
    string AcademicYear
    datetime DueDateUtc
  }
  FeePayment {
    Guid Id PK
    Guid StudentId
    Guid FeeStructureId FK
    string ClassId
    string Description
    string PeriodLabel
    decimal TotalAmount
    decimal PaidAmount
    bool IsWaiverRequested
    bool IsWaiverApproved
    decimal WaiverAmount
    Guid WaiverApprovedByUserId
  }
  PaymentTransaction {
    Guid Id PK
    Guid FeePaymentId FK
    decimal Amount
    string ReceiptNumber UK
    string PaymentMethod
    string GatewayReference
    datetime PaidAtUtc
    string ReceiptBlobPath
  }
```

`FeeStructureId` is nullable — null for ad-hoc dues. `FeePayment.Status` is a computed C# property (`PaidAmount + WaiverAmount >= TotalAmount ? Paid : ...`), not a stored column.

## Communication module — ER

```mermaid
erDiagram
  Announcement {
    Guid Id PK
    string Title
    string Body
    string TargetRolesCsv
    string TargetClassId
    Guid PostedByUserId
    datetime PublishedAtUtc
    datetime ExpiresAtUtc
  }
  ParentMessage {
    Guid Id PK
    Guid StudentId
    Guid SenderUserId
    Guid RecipientUserId
    string Body
    bool IsRead
    datetime ReadAtUtc
  }
```

## Library module — ER

```mermaid
erDiagram
  Book ||--o{ BookIssue : issued_as
  Book {
    Guid Id PK
    string Isbn
    string Title
    string Author
    string Category
    int TotalCopies
    int AvailableCopies
  }
  BookIssue {
    Guid Id PK
    Guid BookId FK
    Guid StudentId
    datetime IssuedAtUtc
    datetime DueDateUtc
    datetime ReturnedAtUtc
    decimal FineAmount
    bool IsFinePaid
  }
```

`AvailableCopies` is decremented/incremented via `ExecuteUpdateAsync` on issue/return — the same atomic pattern as Fee and Attendance.

## Transport module — ER

```mermaid
erDiagram
  Route ||--o{ Vehicle : served_by
  Route ||--o{ StudentRouteMapping : carries
  Route {
    Guid Id PK
    string Name
    string StartPoint
    string EndPoint
    decimal MonthlyFee
  }
  Vehicle {
    Guid Id PK
    string RegistrationNumber
    int Capacity
    string DriverName
    string DriverPhone
    Guid RouteId FK
  }
  StudentRouteMapping {
    Guid Id PK
    Guid StudentId UK
    Guid RouteId FK
    string PickupPoint
    datetime AssignedAtUtc
  }
```

Unique index on StudentId in the mapping — a student can only be on one route at a time.

## Notification module — ER

```mermaid
erDiagram
  NotificationLog {
    Guid Id PK
    string EventType
    string RecipientReference
    string DispatchChannel
    string PayloadJson
    bool IsDelivered
    datetime DeliveredAtUtc
    string FailureReason
  }
```

One durable row per consumed event across all 4 MassTransit consumers — UserRegisteredConsumer, StudentEnrolledConsumer, FeePaidConsumer, CertificateGeneratedConsumer.

## Reporting module — ER

```mermaid
erDiagram
  ReportSnapshot {
    Guid Id PK
    string ReportType
    datetime FromUtc
    datetime ToUtc
    string ResultJson
    Guid GeneratedByUserId
  }
```

Reporting owns only its own generated-report history; the underlying figures are always fetched fresh through the Student and Fee business services, never by querying their tables.

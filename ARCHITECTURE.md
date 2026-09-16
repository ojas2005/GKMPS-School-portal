# GKMPS School ERP — Architecture Diagrams

HLD · LLD · per-service ER · 12 ASP.NET Core 9 microservices · YARP gateway `:5100` · TiDB · Redis · RabbitMQ/MassTransit + Outbox

> Diagrams are Mermaid source, rendered natively by GitHub — no external tools needed to view them.

## Contents

**HLD**
- [1. System architecture](#1-system-architecture)
- [2. Event flow & Reporting's synchronous fan-out](#2-event-flow--reportings-synchronous-fan-out)

**LLD**
- [3. Per-service layering (5-tier)](#3-per-service-layering-5-tier)
- [4. Auth: login, lockout, refresh rotation](#4-auth-login-lockout-refresh-rotation-self-service-scoping)
- [5. The atomic-update pattern](#5-the-atomic-update-pattern-repeated-across-4-services)

**ER** — [Identity](#identityapi--er) · [Student](#studentapi--er) · [Staff](#staffapi--er) · [Attendance](#attendanceapi--er) · [Academic](#academicapi--er) · [Examination](#examinationapi--er) · [Fee](#feeapi--er) · [Communication](#communicationapi--er) · [Library](#libraryapi--er) · [Transport](#transportapi--er) · [Notification](#notificationapi--er) · [Reporting](#reportingapi--er)

---

## 1. System architecture

12 services behind a YARP gateway (`:5100`), each with its own TiDB (MySQL-wire-protocol) database. Redis backs distributed caching and a fast refresh-token revocation check. RabbitMQ + MassTransit carries 4 integration events, with a transactional **Outbox** on Identity/Student/Fee.

```mermaid
flowchart TB
  SPA["Angular SPA :4200"]:::client
  CAD["Caddy (TLS termination)"]:::gw
  GW["SchoolERP.Gateway (YARP) :5100<br/>JWT · CORS · rate limit · health aggregation"]:::gw
  subgraph SV["12 microservices — each with its own TiDB DB"]
    direction LR
    ID["Identity.API :5101"]:::svc
    ST["Student.API :5102"]:::svc
    SF["Staff.API :5103"]:::svc
    AT["Attendance.API :5104"]:::svc
    AC["Academic.API :5105"]:::svc
    EX["Examination.API :5106"]:::svc
    FE["Fee.API :5107"]:::svc
    CO["Communication.API :5108"]:::svc
    LI["Library.API :5109"]:::svc
    TR["Transport.API :5110"]:::svc
    NO["Notification.API :5111"]:::svc
    RE["Reporting.API :5112"]:::svc
  end
  MQ(["RabbitMQ<br/>MassTransit + EF Outbox on Identity/Student/Fee"]):::infra
  DB[("TiDB (MySQL protocol)<br/>one schema per service")]:::infra
  RD[("Redis<br/>cache + refresh-token revocation check")]:::infra
  BLOB[("Azure Blob Storage<br/>SAS-signed PDFs via QuestPDF")]:::infra

  SPA -->|HTTPS| CAD --> GW
  GW -->|"/api/* path routes"| SV
  ID --> DB
  ST --> DB
  ID -.outbox publish.-> MQ
  ST -.outbox publish.-> MQ
  FE -.outbox publish.-> MQ
  MQ -.consume.-> NO
  RE -->|"Polly retry + circuit breaker"| ST
  RE -->|"Polly retry + circuit breaker"| FE
  ID --> RD
  AC --> RD
  ST -. SAS URL .-> BLOB
  FE -. SAS URL .-> BLOB
  EX -. SAS URL .-> BLOB

  classDef client fill:#ede9fe,stroke:#7c3aed,color:#4c1d95;
  classDef gw fill:#d1fae5,stroke:#059669,color:#064e3b,font-weight:bold;
  classDef svc fill:#ffffff,stroke:#6ee7b7,color:#0f172a;
  classDef infra fill:#ecfeff,stroke:#0891b2,color:#155e75,font-weight:bold;
```

Only Identity, Student and Fee publish events — via the outbox pattern (event row written in the same DB transaction as the business change, then relayed to RabbitMQ), so a crash between commit and publish can't lose the event.

## 2. Event flow & Reporting's synchronous fan-out

Two communication styles side by side: async fan-out (Notification consumes all 4 events) and sync composition with resilience (Reporting calls Student/Fee directly over HTTP, wrapped in Polly).

```mermaid
flowchart LR
  REG["POST /api/auth/register"] -->|"UserRegisteredEvent (outbox)"| N1["Notification: welcome email"]
  ADM["Admit student"] -->|"StudentEnrolledEvent (outbox)"| N2["Notification: welcome SMS"]
  PAY["POST /api/fee-payments"] -->|"FeePaidEvent (outbox)"| N3["Notification: payment receipt push"]
  CERT["Generate TC / report card / receipt PDF"] -->|"CertificateGeneratedEvent"| N4["Notification: doc-ready alert, verification code only"]

  RPT["GET /api/reports/enrollment"] -->|"HTTP + retry(3, expo backoff)"| ST2["Student.API"]
  RPT -->|"HTTP + circuit breaker(5, 30s)"| FE2["Fee.API"]

  classDef e fill:#ffffff,stroke:#94a3b8,color:#0f172a;
  class REG,ADM,PAY,CERT,N1,N2,N3,N4,RPT,ST2,FE2 e;
```

`CertificateGeneratedEvent` carries a `VerificationCode`, not the SAS URL — the message bus/consumer logs can never leak a working, time-limited link to a certificate.

## 3. Per-service layering (5-tier)

Every one of the 12 services follows exactly the same shape, stated in the README: Entity → Repository Interface → Service Interface → Service Implementation → Controller.

```mermaid
flowchart TB
  C["Controller: attribute routing, Authorize Roles"] --> SI["Service Interface (IXxxService)"]
  SI --> SImpl["Service Implementation: business rules, atomic ExecuteUpdateAsync, event publish"]
  SImpl --> RI["Repository Interface (IXxxRepository)"]
  RI --> R["Repository Implementation: EF Core over TiDB"]
  R --> E["Entities: BaseEntity Id, CreatedAtUtc, UpdatedAtUtc, IsDeleted"]
  SImpl -.publish/consume.-> MQ(["MassTransit / RabbitMQ"])
  classDef n fill:#ecfdf5,stroke:#059669,color:#064e3b;
  class C,SI,SImpl,RI,R,E n;
```

`BaseEntity.IsDeleted` gives every entity soft delete — global query filters exclude it by default. Every service also has an `AuditLog` table (who/what/before/after), fed to Serilog and persisted for queryable history.

## 4. Auth: login, lockout, refresh rotation, self-service scoping

JWT (15 min) + rotating hashed refresh tokens (7 days), PBKDF2 password hashing, account lockout, and role-based scope claims embedded at issue time.

```mermaid
sequenceDiagram
  autonumber
  actor U as User
  participant GW as Gateway
  participant ID as Identity.API
  participant DB as Identity DB (TiDB)
  participant RD as Redis
  U->>GW: POST /api/auth/login loginId, password
  GW->>ID: forward, auth-strict rate limit 60/min/IP
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
      ID->>DB: resolve linked Student/Staff profile
      ID->>ID: mint JWT with role + scope claims
      ID->>DB: store hashed refresh token
      ID->>RD: mirror active refresh-token hash
      ID-->>U: 200 accessToken, refreshToken, scope claims
    end
  end
```

On refresh, presenting an already-revoked token revokes the whole token family for that user (theft-reuse detection) — see `AuthService.RefreshAsync`.

## 5. The atomic-update pattern (repeated across 4 services)

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

## Identity.API — ER

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

## Student.API — ER

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

## Staff.API — ER

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

## Attendance.API — ER

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

## Academic.API — ER

Subjects are relational; timetable slots and the timetable-generator config are stored as JSON blobs, Redis-cached on read.

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

## Examination.API — ER

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

## Fee.API — ER

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

## Communication.API — ER

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

## Library.API — ER

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

## Transport.API — ER

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

## Notification.API — ER

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

## Reporting.API — ER

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

Reporting owns only its own generated-report history; the underlying figures are always fetched fresh from Student.API/Fee.API via Polly-wrapped HttpClients, never joined cross-schema.

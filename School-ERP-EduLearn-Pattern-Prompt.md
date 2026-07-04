# School Management ERP — Angular + .NET Microservices
## (Prompt built on the EduLearn LMS Case-Study Architecture Pattern)

Use this prompt with Claude Code, Cursor, or any AI coding tool. It asks the model to follow the **same layered pattern** used in the EduLearn LMS case study (Entity → Repository Interface → Service Interface → Service Implementation → Controller, per microservice) applied to a School ERP domain, with Angular replacing the Razor MVC layer.

---

## Prompt

> I want to build a **School Management ERP** as a set of independently deployable **ASP.NET Core 9 Web API microservices**, orchestrated behind a **YARP API Gateway**, with an **Angular 18+ SPA** as the client (no Razor views). Follow the same architectural discipline as a mature reference LMS platform I've reviewed: every microservice owns its own EF Core `DbContext` and PostgreSQL schema, and every domain follows a strict **Entity → Repository Interface → Service Interface → Service Implementation → Controller** layering, registered via `builder.Services.AddScoped<IService, ServiceImpl>()` in `Program.cs`.
>
> This is an original build — no proprietary code or branding copied from any commercial ERP.
>
> ## Per-Microservice Pattern (apply to every service below)
>
> For **each** microservice, generate:
> 1. **EF Core Entity** — with explicit attributes, nullable value types (`DateTime?`) where appropriate, and composite/unique indexes called out explicitly (e.g., `(StudentId, ClassId)`, unique index on `AdmissionNumber`)
> 2. **Repository Interface** (`I{Entity}Repository`) — async `Task<T>` methods only: `FindById`, `FindByX`, `Search{X}()` (EF Core `LIKE`), `Count{X}()`, and any atomic-update methods using `ExecuteUpdateAsync`
> 3. **Service Interface** (`I{Entity}Service`) — business-facing methods (Create, Update, workflow transitions, aggregates)
> 4. **Service Implementation** — implements the interface; this is where workflow rules, validation orchestration, and cross-repository coordination live (never in the controller)
> 5. **API Controller** (`[ApiController][Route("api/{resource}")]`, inherits `ControllerBase`) — thin, delegates to the service, returns `IActionResult`
>
> Apply these **specific patterns from the reference architecture** wherever they fit:
> - **Atomic field updates** via EF Core `ExecuteUpdateAsync` instead of load-then-save (e.g., incrementing `Attendance.PresentCount`, `Library.AvailableCopies`, `Fee.PaidAmount`) — never load a full entity just to bump a counter
> - **Two-step workflow flags** (`IsSubmitted` → `IsApproved`) for anything requiring sign-off: leave requests (Teacher submits → Principal/Admin approves), fee waivers, exam result publishing, document verification
> - **Aggregate queries** via EF Core `AverageAsync`/`SumAsync`/`GroupBy` for things like average attendance %, average marks per subject, fee collection totals — never pull all rows into memory to compute in C#
> - **JSON-serialized structured fields** (`System.Text.Json`) for variable-shape data: exam answer keys, timetable slot configs, notification payloads — stored as a `string` column, deserialized in the service layer
> - **Public, unauthenticated verification endpoints** for anything that needs third-party trust without login — e.g., transfer certificate verification by code, bonafide certificate verification — mirroring the LMS's `VerifyCertificate(code)` pattern
> - **PDF generation via QuestPDF**, uploaded to Azure Blob Storage, delivered via **time-limited SAS URLs** — for report cards, transfer certificates, fee receipts, ID cards
> - **Duplicate-prevention checks before insert** (mirroring `HasStudentReviewed()`) — e.g., `HasMarkedAttendanceToday()`, `HasAlreadyEnrolledInSubject()`, `HasSubmittedAssignment()`
>
> ## Microservices
>
> | Microservice | Namespace | Primary Domain |
> |---|---|---|
> | Identity.API | `SchoolERP.Identity` | Users, JWT Bearer + refresh tokens, Google OAuth, role-based access (SuperAdmin/Principal/Admin/Teacher/Student/Parent/Accountant/Librarian) |
> | Student.API | `SchoolERP.Student` | Admissions, profiles, class/section allocation, documents, transfer certificates |
> | Staff.API | `SchoolERP.Staff` | Teacher/staff profiles, subject allocation, leave requests (two-step approval) |
> | Attendance.API | `SchoolERP.Attendance` | Daily/monthly attendance, atomic present/absent counters, late-arrival tracking |
> | Academic.API | `SchoolERP.Academic` | Classes, sections, subjects, timetable, homework, assignments, lesson plans |
> | Examination.API | `SchoolERP.Examination` | Exam creation, marks entry, grade computation, report card generation (QuestPDF), rank generation (aggregate queries) |
> | Fee.API | `SchoolERP.Fee` | Fee structures, collection, atomic paid-amount updates, receipts (QuestPDF), dues, payment gateway integration |
> | Communication.API | `SchoolERP.Communication` | Announcements, parent messaging, SMS/email integration interfaces |
> | Library.API | `SchoolERP.Library` | Books, categories, issue/return, atomic available-copies updates, fine calculation |
> | Transport.API | `SchoolERP.Transport` | Routes, vehicles, drivers, student route mapping |
> | Notification.API | `SchoolERP.Notification` | Consumes MassTransit events from all services, dispatches push/email/SMS |
> | Reporting.API | `SchoolERP.Reporting` | Cross-service aggregate analytics, PDF/Excel export |
>
> ## Microservices Architecture Overview
> - Each service owns its own EF Core `DbContext` and PostgreSQL schema — **no cross-service database access**
> - Synchronous inter-service calls use `IHttpClientFactory` typed clients wrapped with **Polly** retry + circuit breaker
> - Asynchronous events (fee payment confirmation, certificate generation, bulk notifications, attendance-triggered alerts) use **MassTransit + RabbitMQ**, with the **Outbox Pattern** so events publish reliably alongside the EF Core transaction that created them
> - **YARP API Gateway** in front of all services: JWT validation, request routing, and **rate limiting** (`Microsoft.AspNetCore.RateLimiting`, Redis-backed so limits hold across instances — stricter limits on login/OTP endpoints, standard limits elsewhere)
> - **Redis** (`StackExchange.Redis`) as `IDistributedCache` for: dashboard stats (5-min TTL), timetable lookups, JWT refresh token store, rate-limit counters
>
> ## Non-Functional Requirements (match the reference bar)
>
> | Category | Requirement |
> |---|---|
> | Security | `PasswordHasher<User>` (PBKDF2+HMAC-SHA256); JWT Bearer with short expiry + rotated refresh tokens; `[Authorize(Roles)]` on every protected endpoint; Blob content served only via SAS tokens; HTTPS enforced |
> | Performance | Composite indexes on hot lookup paths (`(StudentId, ClassId)`, `(TeacherId, Date)` for attendance); Redis cache on read-heavy dashboard/report endpoints |
> | Scalability | Each service independently scalable via Docker/Kubernetes; PDF generation and bulk notifications offloaded to MassTransit + RabbitMQ async queues; YARP for routing and load balancing |
> | Data Integrity | Unique indexes preventing duplicate records (one attendance record per student per day, one admission number per student); EF Core transactions wrap multi-step writes (e.g., fee payment + receipt + ledger update) atomically |
> | Availability | `/health`, `/health/ready`, `/health/live` via `Microsoft.Extensions.Diagnostics.HealthChecks`, aggregated at the gateway; Polly retry + circuit breaker on all inter-service calls |
> | Audit Trail | All admin actions, approvals, and record deletions logged to an `AuditLog` entity (actor, timestamp, before/after state) via `ILogger<T>` (Serilog) |
>
> ## Proposed Technology Stack
>
> | Layer | Technology |
> |---|---|
> | Backend Framework | ASP.NET Core 9 Web API, Kestrel |
> | Authentication | `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.AspNetCore.Authentication.Google`, `PasswordHasher<T>` |
> | ORM / Data Access | EF Core 9, `Npgsql.EntityFrameworkCore.PostgreSQL`, EF Core Migrations, `ExecuteUpdateAsync` for atomic updates |
> | Database | PostgreSQL (one schema per service); Redis (`StackExchange.Redis`) for cache + session |
> | File Storage | Azure Blob Storage (`Azure.Storage.Blobs`), SAS URLs for documents/certificates/receipts |
> | PDF Generation | QuestPDF — report cards, transfer certificates, fee receipts, ID cards |
> | Messaging | `MassTransit.RabbitMQ` for async events (fee confirmation, certificate generation, notification fan-out); Outbox pattern for reliable publishing |
> | Inter-Service HTTP | `IHttpClientFactory` typed clients + `Microsoft.Extensions.Http.Polly` for retry/circuit breaker |
> | API Gateway | YARP (`Yarp.ReverseProxy`) — routing, JWT validation, `Microsoft.AspNetCore.RateLimiting` |
> | Frontend | Angular 18+ standalone components, TypeScript strict mode, NgRx (or SignalStore), Tailwind + Angular Material, RxJS, lazy-loaded feature modules per role |
> | Logging | `Serilog.AspNetCore` → Seq or Azure Application Insights |
> | API Docs | `Swashbuckle.AspNetCore` per service + aggregated gateway-level Swagger UI, API versioning (`Asp.Versioning`) |
> | Containerization | Docker multi-stage builds, Docker Compose for local dev, Kubernetes for production with HPA on Attendance and Examination services (peak-load services) |
> | CI/CD | GitHub Actions: `dotnet restore` → `build` → `test` → Docker build → push → rolling deploy |
>
> ## Delivery Approach
> Build service-by-service, and for **each** service produce the same five artifacts described above (Entity, Repository Interface, Service Interface, Service Implementation, Controller) plus a matching Angular feature module. Start with:
> 1. **Identity.API** (JWT + refresh + Google OAuth + RBAC + rate limiting) + Angular auth module and layout shell
> 2. **Student.API** (full CRUD, atomic admission-count updates, transfer certificate PDF + SAS delivery) + Angular student management module
> 3. Remaining services one at a time, each fully following the layered pattern, with RabbitMQ events wired for cross-service effects (e.g., `StudentEnrolled` → Notification service sends welcome email; `FeePaid` → Reporting service updates collection totals)
>
> For every service also generate: Docker-ready project structure, EF Core migrations, Swagger docs, xUnit + Moq unit tests for the service layer, Jasmine/Karma or Jest tests for the Angular module, and a short README documenting endpoints and events published/consumed.

---

### What was carried over from the reference case study
- The strict 5-layer pattern per microservice (rather than a generic "controller + DbContext" shortcut)
- `ExecuteUpdateAsync` for every counter/flag update instead of load-then-save
- Two-step submit/approve workflows for anything needing sign-off
- Public verification endpoints for certificates without requiring login
- QuestPDF + Blob + SAS URL as the standard document-delivery chain
- Duplicate-prevention guard methods before every insert that could be double-submitted
- The specific non-functional bar (indexes, audit logs, health checks, Polly, rate limiting) stated as requirements, not left implicit

# GKMPS School ERP — Backend

ASP.NET Core 9 microservices backend for the school's management system, built for
data security, reliability, and accessibility as the guiding priorities. This is the
**backend only** — the Angular frontend comes next.

All 12 microservices from the design doc are implemented, each following the same
five-layer pattern: **Entity → Repository Interface → Service Interface → Service
Implementation → Controller**, registered via `AddScoped<IService, ServiceImpl>()`.

```
SchoolERP.sln
BuildingBlocks/
  SchoolERP.Shared/          # BaseEntity, AuditLog, ApiResponse, RoleNames, cross-service event contracts
Services/
  Identity.API/               # Accounts, JWT + refresh rotation, Google OAuth, RBAC
  Student.API/                # Admissions, class/section management, transfer certificates
  Staff.API/                  # Staff profiles, two-step leave approval workflow
  Attendance.API/             # Daily attendance, atomic present/absent/late counters
  Academic.API/                # Subjects, JSON-based timetables, homework
  Examination.API/            # Exams, marks entry, aggregates, QuestPDF report cards
  Fee.API/                    # Fee structures, atomic paid-amount updates, receipts, waivers
  Communication.API/          # Announcements, parent messaging
  Library.API/                # Books, atomic available-copies, issue/return, fines
  Transport.API/              # Routes, vehicles, student-route mapping
  Notification.API/           # MassTransit consumers -> email/SMS/push fan-out
  Reporting.API/               # Cross-service aggregates via Polly-wrapped HTTP clients, PDF export
Gateway/
  SchoolERP.Gateway/          # YARP: routing, JWT validation, rate limiting, health aggregation
scripts/                      # 01-13: hand-authored SQL matching each service's EF model (see "Migrations" below)
docker-compose.yml
.env.example
```

## Running locally

1. Install the .NET 9 SDK and Docker.
2. `cp .env.example .env` and fill in a real `JWT_SIGNING_KEY` (32+ random characters)
   and Postgres/RabbitMQ passwords.
3. `docker compose up --build` — this builds and starts Postgres, Redis, RabbitMQ, all
   12 services, and the gateway.
4. Gateway: `http://localhost:5100` · RabbitMQ management UI: `http://localhost:15672`

Each service also exposes Swagger directly (left on in every environment specifically
so you can test APIs without extra setup — see "Testing the APIs" below):

| Service | Direct port | Swagger |
|---|---|---|
| Identity.API | 5101 | http://localhost:5101/swagger |
| Student.API | 5102 | http://localhost:5102/swagger |
| Staff.API | 5103 | http://localhost:5103/swagger |
| Attendance.API | 5104 | http://localhost:5104/swagger |
| Academic.API | 5105 | http://localhost:5105/swagger |
| Examination.API | 5106 | http://localhost:5106/swagger |
| Fee.API | 5107 | http://localhost:5107/swagger |
| Communication.API | 5108 | http://localhost:5108/swagger |
| Library.API | 5109 | http://localhost:5109/swagger |
| Transport.API | 5110 | http://localhost:5110/swagger |
| Notification.API | 5111 | http://localhost:5111/swagger |
| Reporting.API | 5112 | http://localhost:5112/swagger |

## Authentication model — owner-issued accounts, no self-registration

There is no public sign-up. `POST /api/auth/register` requires a SuperAdmin/Principal/
Admin bearer token — only the school owner (or someone they've granted admin rights to)
can create accounts. On first startup, Identity.API seeds one bootstrap account:

- **Username**: `ownerishim`
- **Password**: `Owner@1234`
- (override via `Owner:Username` / `Owner:Password` config before first run)

Login accepts either the username or the email in the same `loginId` field
(`POST /api/auth/login { "loginId": "...", "password": "..." }`). The owner creates every
student/teacher account (choosing their login ID and password) and hands the credentials
over directly — there's no forgot-password flow yet.

## Testing the APIs

The fastest path: open any service's Swagger URL above, log in as the owner via
`POST /api/auth/login` on Identity.API (see credentials above), copy the `accessToken`
from the response, click **Authorize** in Swagger, paste `Bearer <token>`, and call any
endpoint — including `POST /api/auth/register` to create further accounts. All Swagger
UIs are reachable directly on their service port; the gateway also proxies each one at
`/{service}/swagger` (e.g. `http://localhost:5100/student/swagger`) once behind the
gateway.

Public endpoints that don't need a token: `POST /api/auth/login`, `POST
/api/auth/login/google`, `POST /api/auth/refresh`, and `GET
/api/transfer-certificates/verify/{code}`.

## Migrations

Every service now has a real `Migrations/` (or `Data/Migrations/`) folder containing an
`InitialCreate` migration, and every `Program.cs` calls `db.Database.Migrate()` on
startup, so `docker compose up` creates each service's schema automatically. Their
provenance differs, though, and it's worth knowing which is which:

- **Identity.API and Student.API** carry migrations actually produced by `dotnet ef
  migrations add` (a background tooling pass in this environment briefly had SDK
  access partway through the build — visible from the `ProductVersion "9.0.0"`
  annotation in their `*.Designer.cs`/`*ModelSnapshot.cs` files, which a hand-written
  migration wouldn't have). These are authoritative and include MassTransit's EF Core
  Outbox tables (`InboxState`/`OutboxMessage`/`OutboxState`), which is why those two
  services -- plus Fee.API, below -- have the Outbox pattern fully wired in
  (`AddEntityFrameworkOutbox<TDbContext>()` in `Program.cs`).
- **Fee.API** has a hand-authored migration, but its `InboxState`/`OutboxMessage`/
  `OutboxState` tables are copied column-for-column from Identity.API's real one
  (rather than re-guessed), so its Outbox is also fully wired in with the same
  confidence.
- **The other 9 services** (Staff, Attendance, Academic, Examination, Communication,
  Library, Transport, Notification, Reporting) have hand-authored migrations with no
  SDK available to verify them against. They don't publish through the Outbox (none of
  them call `AddEntityFrameworkOutbox`), so there's no MassTransit schema to get wrong
  there -- these migrations are plain `CreateTable`/`CreateIndex`/`ForeignKey` calls
  mirroring each `DbContext.OnModelCreating` closely enough to review by inspection.

Before a real production rollout, verify the hand-authored migrations with the real
tool once you have the SDK:

```bash
dotnet tool install --global dotnet-ef
cd Services/Staff.API && dotnet ef migrations add VerifyInitialCreate
# if it comes back empty (no model changes detected), the hand-written migration matches
# the model exactly and you're done; if not, review the diff it proposes.
```

`scripts/01-schemas.sql` (which only creates the 12 empty PostgreSQL schemas) is still
mounted into the `postgres` container at first boot; `scripts/02-*.sql` through
`13-*.sql` are no longer mounted (`docker-compose.yml` only mounts `01-schemas.sql`
now) since real migrations own table creation and would conflict with them. Those
files are kept in the repo for reference only.

## Per-service endpoint summary

**Identity.API** — `POST /api/auth/{register,login,login/google,refresh,logout}`,
`GET/PATCH /api/users`. PBKDF2+HMAC-SHA256 password hashing, 15-min JWTs with rotated
7-day refresh tokens (hashed at rest), Google ID-token verification, role claims for
every other service's `[Authorize(Roles=...)]`. Publishes `UserRegisteredEvent`.

**Student.API** — admissions (`POST /api/students`), atomic class reassignment
(`PATCH /api/students/{id}/class`), Redis-cached enrollment stats (5-min TTL), transfer
certificates with a two-step submit/approve workflow, QuestPDF generation → Azure Blob
→ 15-minute SAS URL, and a **public, unauthenticated** verification endpoint (`GET
/api/transfer-certificates/verify/{code}`). Publishes `StudentEnrolledEvent`,
`CertificateGeneratedEvent`.

**Staff.API** — staff onboarding, two-step leave request submit/approve workflow with
overlapping-date duplicate-prevention.

**Attendance.API** — one record per student per day (unique index), atomic
present/absent/late monthly counters via `ExecuteUpdateAsync`, attendance-percentage
aggregate.

**Academic.API** — subjects, JSON-serialized timetable slots (`System.Text.Json`,
Redis-cached 5-min TTL), homework assignment.

**Examination.API** — exams with JSON answer keys, marks entry with duplicate
prevention + atomic correction, `AverageAsync`/`GroupBy`-based rankings, two-step
result-publish workflow, QuestPDF report cards → Blob → SAS URL.

**Fee.API** — fee structures, atomic `PaidAmount` increments on every payment
(`ExecuteUpdateAsync`, never load-then-save), immutable payment-transaction ledger,
QuestPDF receipts → Blob → SAS URL, two-step fee-waiver workflow, `SumAsync`-based
collection totals. Publishes `FeePaidEvent`.

**Communication.API** — role/class-targeted announcements, parent↔teacher messaging
with an atomic read-flag flip.

**Library.API** — atomic `AvailableCopies` increment/decrement on issue/return
(`ExecuteUpdateAsync`), duplicate-issue prevention, days-late fine calculation.

**Transport.API** — routes, vehicles, one active route mapping per student (unique
index), atomic reassignment.

**Notification.API** — pure MassTransit consumer: subscribes to `UserRegisteredEvent`,
`StudentEnrolledEvent`, `FeePaidEvent`, `CertificateGeneratedEvent` from every other
service and fans them out via `IDispatchService` (Email/SMS/Push). The default
`LoggingDispatchService` implementation logs instead of calling a real provider — swap
it for a SendGrid/Twilio/FCM-backed implementation in `Program.cs` with no other code
changes. `GET /api/notifications` returns delivery history per recipient.

**Reporting.API** — cross-service aggregates fetched live via `IHttpClientFactory`
typed clients wrapped in Polly retry (3 attempts, exponential backoff) + circuit
breaker (opens after 5 failures, 30s reset), never by querying another service's
database directly. Every report run is persisted as a `ReportSnapshot` for
reproducibility. `GET /api/reports/enrollment`, `GET /api/reports/enrollment/pdf`, `GET
/api/reports/fee-collection`.

## Security & reliability choices (why they're here)

- **Data security**: passwords are never stored in plain text; refresh tokens are
  stored as SHA-256 hashes, not raw values; generated documents (certificates, report
  cards, receipts) are served only via time-limited SAS URLs, never public blob links;
  every protected endpoint requires a role-checked JWT; notification payloads carry
  verification codes, never raw SAS URLs.
- **Reliability**: Polly retry + circuit breaker on Reporting.API's inter-service HTTP
  calls; RabbitMQ decouples slow/failing consumers (Notification.API) from the services
  that publish events; health checks (`/health`, `/health/ready`, `/health/live`) are
  exposed by every service and aggregated at the gateway.
- **Accessibility**: a consistent `ApiResponse<T>` envelope across every endpoint in
  every service, so the Angular client can render success/error states uniformly no
  matter which service answered; centralized `RoleNames` avoid typo-based access bugs.
- **Audit trail**: admin actions (status changes, class reassignment, leave/TC/waiver
  approvals) log actor, role, and before/after state via Serilog across every service
  that has an approval workflow.

## A note on verification

This code was written directly (no .NET SDK was available in the build sandbox to run
`dotnet build`/`dotnet test`), so it hasn't been compiled here. Every file was
hand-reviewed for namespace consistency, correct EF Core 9 / ASP.NET Core 9 API usage,
and pattern consistency across all 12 services — this pass caught and fixed several
real issues (a missing `using` in `UserService.cs`, a blocking `.Result` call in
`MarksRepository`, and a case-sensitivity bug in `Reporting.API`'s JSON deserialization
of other services' camelCase responses). **Before deploying, run `dotnet build` and
`dotnet test` locally** — that's the one verification step still outstanding.

## Next steps

1. Run `dotnet ef migrations add VerifyInitialCreate` against the 9 hand-authored
   migrations (see "Migrations" above) to confirm they match their `DbContext`s exactly.
2. Add xUnit + Moq test projects per service.
3. Build the Angular 18+ SPA (auth module + layout shell first, per the delivery plan),
   with a feature module per service.
4. Wire real providers behind `Notification.API`'s `IDispatchService` (SendGrid/Twilio/FCM)
   and behind `Student.API`/`Fee.API`/`Examination.API`'s `IBlobStorageService` (a real
   Azure Storage account, or swap for S3/GCS with the same interface).

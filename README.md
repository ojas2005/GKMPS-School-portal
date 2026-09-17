<div align="center">
<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:6366F1,100:22D3EE&height=200&section=header&text=GKMPS%20School%20ERP&fontSize=42&fontColor=ffffff&animation=fadeIn&fontAlignY=35&desc=ASP.NET%20Core%209%20N-Tier%20Backend&descAlignY=55&descSize=16" />

<a href="https://github.com/ojas2005/GKMPS-School-portal/actions/workflows/backend-ci.yml">
  <img src="https://readme-typing-svg.demolab.com/?font=Fira+Code&size=20&pause=1000&color=6366F1&center=true&vCenter=true&width=650&lines=One+app%2C+four+tiers%2C+twelve+modules;JWT+auth+%2B+RBAC+%2B+event-driven+notifications;Owner-issued+accounts.+No+public+sign-up.;Built+with+ASP.NET+Core+9+%2B+TiDB" alt="Typing SVG" />
</a>

<br/>

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![TiDB](https://img.shields.io/badge/TiDB-MySQL_compatible-DD0031?style=for-the-badge&logo=mysql&logoColor=white)
![Azure](https://img.shields.io/badge/Azure-Blob_Storage-0078D4?style=for-the-badge&logo=microsoftazure&logoColor=white)
![CI](https://img.shields.io/github/actions/workflow/status/ojas2005/GKMPS-School-portal/backend-ci.yml?style=for-the-badge&label=CI&logo=githubactions&logoColor=white)
![License](https://img.shields.io/badge/license-Unlicensed-lightgrey?style=for-the-badge)

</div>

## About

**GKMPS School ERP** is the backend for a school management system: a single ASP.NET Core 9
application built in **n-tier** layers, with **data security**, **reliability** and
**accessibility** as the guiding priorities. It covers everything a school runs on day to
day — admissions, staff, attendance, academics, examinations, fees, communication, library
and transport — plus notifications tying it all together. This is the **backend only**; the
[Angular frontend](https://github.com/ojas2005/GKMPS-Frntend) talks to it over `/api/*`.

It runs comfortably on ~200 MB of RAM, which keeps hosting free or close to it for a school
of this size (~1,300 accounts, a few hundred daily users).

<div align="center">
<img src="https://readme-typing-svg.demolab.com/?font=Fira+Code&size=14&pause=1500&color=94A3B8&center=true&vCenter=true&width=700&lines=cp+.env.example+.env+%26%26+docker+compose+up+--build;Open+http%3A%2F%2Flocalhost%3A5100" alt="quickstart typing" />
</div>

## Tiers

| Project | Tier | Contains |
|---|---|---|
| `src/SchoolERP.Api` | Presentation | Controllers, JWT auth, CORS, rate limiting, Swagger, health checks, startup (migrations + owner seed) |
| `src/SchoolERP.Business` | Business | Services and their interfaces, DTOs, token issuing, QuestPDF documents, notification event handlers, in-process event bus |
| `src/SchoolERP.DataAccess` | Data access | Entities, one EF Core `DbContext` + migrations per module, repositories, Azure Blob Storage |
| `src/SchoolERP.Common` | Cross-cutting | `ApiResponse`, `RoleNames`, `CallerClaims`, `BaseEntity`/`AuditLog`, event contracts, exception handling, logging, security headers, hosting helpers |

References only point downward: **Api → Business → DataAccess → Common**.

## Modules

Every tier is organised by the same 12 school modules. Each module keeps its **own
database** (`identity`, `student`, `fee`, ...) on one TiDB/MySQL server.

| Module | Owns | Raises |
|---|---|---|
| Identity | Accounts, JWT + refresh rotation, role hierarchy, lockout | `UserRegisteredEvent` |
| Student | Admissions, class moves, parent logins, transfer certificates | `StudentEnrolledEvent`, `CertificateGeneratedEvent` |
| Staff | Staff profiles, salaries and payouts, leave, staff attendance | — |
| Attendance | Daily attendance, atomic present/absent/late counters | — |
| Academic | Subjects, timetables and the timetable generator, homework | — |
| Examination | Exams, marks, rankings, QuestPDF report cards | — |
| Fee | Fee structures, dues, atomic payments, receipts, waivers | `FeePaidEvent` |
| Communication | Announcements, parent ↔ teacher messages | — |
| Library | Books, issue/return, fines | — |
| Transport | Routes, vehicles, student-route mapping | — |
| Notification | Handles every event above: records it and sends email when SMTP is configured | handles all |
| Reporting | Enrollment and fee-collection reports, PDF export | — |

Modules call each other's **business services** in-process (for example Reporting uses the
Student and Fee services), never each other's tables.

## Tech stack

- **Framework** — ASP.NET Core 9, EF Core 9 (Pomelo MySQL provider)
- **Data** — TiDB Cloud (MySQL-compatible; one database per module), in-memory caching
- **Events** — in-process event bus with a background dispatcher (no message broker needed)
- **Auth** — PBKDF2 password hashing, 15-min JWTs, rotated 7-day refresh tokens (hashed at
  rest), account lockout, role hierarchy for account management
- **Files** — QuestPDF receipts, report cards and certificates in Azure Blob Storage,
  shared via 15-minute SAS links
- **Ops** — Serilog (console + optional Seq), health checks, per-user rate limiting,
  Swagger (off in production unless enabled), Docker, Caddy (automatic TLS), GitHub Actions CI

## Getting started

Requires the .NET 9 SDK and Docker.

```bash
# configure environment
cp .env.example .env
#   JWT_SIGNING_KEY: openssl rand -hex 32
#   Database: TIDB_* for TiDB Cloud, or uncomment the local block
#             (COMPOSE_PROFILES=local-db,local-storage, TIDB_HOST=tidb, TIDB_SSL_MODE=None, ...)

# build & run the app, Caddy, and (with the local profiles) a TiDB container + Azurite
docker compose up --build
```

The app applies every module's database migrations on startup, retrying while the database
comes up, so no manual ordering is needed.

Then open **http://localhost:5100**. Swagger is off outside Development; set
`Swagger__Enabled=true` to turn it on at `/swagger`. On first boot the app seeds a
`SuperAdmin` owner account (`ownerishim` by default, override via `Owner:Username`) — if
`Owner:Password` / `OWNER_PASSWORD` isn't set, a random password is generated and printed
once in the logs (`docker compose logs app | grep generated`); log in with it and change it
from **My account**. There's no public sign-up — the owner creates further accounts.

## Commands

| Command | What it does |
|---|---|
| `docker compose up --build` | Build and start the app, Caddy and the enabled local services |
| `docker compose up -d` | Same, detached |
| `docker compose logs -f app` | Tail the app's logs |
| `docker compose down` | Stop everything |
| `dotnet build SchoolERP.sln` | Compile without Docker |
| `dotnet test SchoolERP.sln` | Run the unit tests (`Tests/`) |
| `dotnet ef migrations add <Name> -p src/SchoolERP.DataAccess -s src/SchoolERP.Api -c <Module>DbContext -o <Module>/Migrations` | Add an EF Core migration for one module |

## Deploying

- The whole backend is one container image (`Dockerfile` at the repo root) listening on
  port 8080. It needs `ConnectionStrings__SchoolDb` (the TiDB server, without a database
  name), `ConnectionStrings__BlobStorage`, `Jwt__SigningKey` and `Cors__AllowedOrigins__0`
  (the frontend origin); see `docker-compose.yml` for the full list.
- With Docker Compose on a server: `docker compose --env-file .env.production up -d --build`
  with its own secrets, **without** the local profiles. Set `DOMAIN` so Caddy obtains a real
  certificate, and `FRONTEND_URL` to the frontend's origin.
- Set `OWNER_PASSWORD` for the first boot (or read the generated one from the logs) and
  change it after signing in. Optionally configure `SMTP_*` so emails are actually sent.
- Rate limits are per signed-in user (or client IP when anonymous). The app trusts
  `X-Forwarded-For` from the proxy in front of it, so don't expose port 8080 directly.

### Current production (Azure)

| Piece | Where |
|---|---|
| Backend | Azure Container Apps `gkmps-api` (resource group `gkmps-prod`, Korea Central; 0.25 vCPU / 0.5 GB, scales to zero) |
| Image registry | Azure Container Registry `gkmpsregistry6333` |
| Frontend | Azure Static Web Apps `gkmps-portal` (Free, East Asia) |
| Database | TiDB Cloud Starter (AWS Tokyo) |
| Files | Azure Blob Storage `blobschool` |

Secrets (database, blob storage, JWT key, owner seed password) are Container Apps secrets, not
image or repo contents. The subscription blocks ACR Tasks, so images are built locally —
the Dockerfile cross-compiles, so this works from Apple Silicon too:

```bash
az acr login -n gkmpsregistry6333
docker buildx build --platform linux/amd64 -t gkmpsregistry6333.azurecr.io/schoolerp-app:<tag> --push .
az containerapp update -g gkmps-prod -n gkmps-api --image gkmpsregistry6333.azurecr.io/schoolerp-app:<tag>
```

## Architecture

See [`ARCHITECTURE.md`](./ARCHITECTURE.md) for the system diagram, events and cross-module
calls, the n-tier layering, the auth/lockout/refresh sequence, the atomic-update pattern, and
an ER diagram for every module.

## Project layout

```
SchoolERP.sln
Dockerfile                        # the single app image
docker-compose.yml                # app + Caddy (+ local TiDB / Azurite / Seq profiles)
src/
  SchoolERP.Api/                  # presentation tier
    Controllers/<Module>/
    Startup/                      # migrations + owner seed
    Program.cs
  SchoolERP.Business/             # business tier
    <Module>/Services/            # + Interfaces/
    <Module>/DTOs/
    <Module>/Documents/           # QuestPDF (Fee, Student, Examination, Reporting)
    Identity/Auth/                # tokens, student/staff profile resolvers
    Notification/Handlers/        # event handlers
    Common/Events/                # in-process event bus
  SchoolERP.DataAccess/           # data access tier
    <Module>/<Module>DbContext.cs
    <Module>/Entities/
    <Module>/Repositories/        # + Interfaces/
    <Module>/Migrations/
    Storage/                      # Azure Blob Storage
  SchoolERP.Common/               # cross-cutting
Tests/
  SchoolERP.Tests/                # xUnit
scripts/                          # legacy Postgres schema scripts, kept for reference only
```

---

<div align="center">
<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:22D3EE,100:6366F1&height=100&section=footer" />
</div>

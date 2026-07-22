<div align="center">
<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:6366F1,100:22D3EE&height=200&section=header&text=GKMPS%20School%20ERP&fontSize=42&fontColor=ffffff&animation=fadeIn&fontAlignY=35&desc=ASP.NET%20Core%209%20Microservices%20Backend&descAlignY=55&descSize=16" />

<a href="https://github.com/ojas2005/GKMPS-School-portal/actions/workflows/backend-ci.yml">
  <img src="https://readme-typing-svg.demolab.com/?font=Fira+Code&size=20&pause=1000&color=6366F1&center=true&vCenter=true&width=650&lines=12+microservices+behind+one+API+gateway;JWT+auth+%2B+RBAC+%2B+event-driven+notifications;Owner-issued+accounts.+No+public+sign-up.;Built+with+ASP.NET+Core+9+%2B+TiDB+%2B+RabbitMQ" alt="Typing SVG" />
</a>

<br/>

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![TiDB](https://img.shields.io/badge/TiDB-MySQL_compatible-DD0031?style=for-the-badge&logo=mysql&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-cache-DC382D?style=for-the-badge&logo=redis&logoColor=white)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-events-FF6600?style=for-the-badge&logo=rabbitmq&logoColor=white)
![CI](https://img.shields.io/github/actions/workflow/status/ojas2005/GKMPS-School-portal/backend-ci.yml?style=for-the-badge&label=CI&logo=githubactions&logoColor=white)
![License](https://img.shields.io/badge/license-Unlicensed-lightgrey?style=for-the-badge)

</div>

## About

**GKMPS School ERP** is the backend for a school management system: 12 ASP.NET Core 9
microservices behind a single YARP gateway, built for **data security**, **reliability**,
and **accessibility** as the guiding priorities. It covers everything a school runs on day
to day — admissions, staff, attendance, academics, examinations, fees, communication,
library, and transport — plus event-driven notifications tying it all together. This is
the **backend only**; the [Angular frontend](https://github.com/ojas2005/GKMPS-Frntend)
talks to it through the gateway.

All 12 services follow the same layering: `Entity` → `Repository Interface` → `Service
Interface` → `Service Implementation` → `Controller`.

<div align="center">
<img src="https://readme-typing-svg.demolab.com/?font=Fira+Code&size=14&pause=1500&color=94A3B8&center=true&vCenter=true&width=700&lines=cp+.env.example+.env+%26%26+docker+compose+up+--build;Open+http%3A%2F%2Flocalhost%3A5100" alt="quickstart typing" />
</div>

## Services

| Service | Port | Owns | Publishes |
|---|---|---|---|
| Identity.API | 5101 | Accounts, JWT + refresh rotation, Google OAuth, RBAC | `UserRegisteredEvent` |
| Student.API | 5102 | Admissions, class/section moves, transfer certificates | `StudentEnrolledEvent`, `CertificateGeneratedEvent` |
| Staff.API | 5103 | Staff profiles, two-step leave approval | — |
| Attendance.API | 5104 | Daily attendance, atomic present/absent/late counters | — |
| Academic.API | 5105 | Subjects, JSON timetables (Redis-cached), homework | — |
| Examination.API | 5106 | Exams, marks entry, rankings, QuestPDF report cards | — |
| Fee.API | 5107 | Fee structures, atomic payments, receipts, waivers | `FeePaidEvent` |
| Communication.API | 5108 | Announcements, parent↔teacher messaging | — |
| Library.API | 5109 | Books, atomic issue/return, fines | — |
| Transport.API | 5110 | Routes, vehicles, student-route mapping | — |
| Notification.API | 5111 | Fans out every event above via Email/SMS/Push | consumes all |
| Reporting.API | 5112 | Cross-service aggregates (Polly retry + circuit breaker), PDF export | — |

Everything sits behind **SchoolERP.Gateway** (YARP: routing, JWT validation, rate
limiting, health aggregation) at `http://localhost:5100`. Each service also exposes
Swagger directly on its own port, and via the gateway at `/{service}/swagger`.

## Tech stack

- **Framework** — ASP.NET Core 9, one microservice per domain, EF Core 9
- **Data** — TiDB Cloud (MySQL-compatible, one database per service), Redis for
  read-through caching
- **Messaging** — RabbitMQ + MassTransit for cross-service events (Outbox pattern on
  Identity/Student/Fee)
- **Gateway** — YARP: routing, JWT validation, rate limiting, aggregated health checks
- **Auth** — PBKDF2+HMAC-SHA256 password hashing, 15-min JWTs, rotated 7-day refresh
  tokens (hashed at rest), Google OAuth
- **Docs/Reliability** — Swagger per service, QuestPDF for certificates/report
  cards/receipts, Polly retry + circuit breaker on Reporting.API's inter-service calls,
  Serilog audit logging
- **Infra** — Docker Compose, Caddy (automatic TLS) in front of the gateway

## Getting started

This backend expects the .NET 9 SDK, Docker, and a TiDB Cloud cluster (the free
Serverless tier is enough to start).

```bash
# configure environment
cp .env.example .env   # fill in JWT_SIGNING_KEY (32+ chars), TIDB_* connection details,
                        # and RabbitMQ credentials

# build & run everything: Redis, RabbitMQ, all 12 services, and the gateway
docker compose up --build
```

Then open **http://localhost:5100** (gateway) or any service's own Swagger port (see
table above). On first boot, Identity.API seeds a `SuperAdmin` owner account
(`ownerishim` by default, override via `Owner:Username`) — if `Owner:Password` /
`OWNER_PASSWORD` isn't set, a random password is generated and printed once in that
container's logs (`docker compose logs identity-api | grep generated`); log in with it
and change it immediately. There's no public sign-up — only the owner can create
further accounts, via `POST /api/auth/register`.

## Commands

| Command | What it does |
|---|---|
| `docker compose up --build` | Build and start every service + gateway + infra |
| `docker compose up -d` | Same, detached |
| `docker compose logs -f <service>` | Tail logs for one service (e.g. `fee-api`) |
| `docker compose down` | Stop everything |
| `dotnet build SchoolERP.sln` | Compile all services without Docker |
| `dotnet ef migrations add <Name>` (run inside a service folder) | Add an EF Core migration |

## Project layout

```
SchoolERP.sln
BuildingBlocks/
  SchoolERP.Shared/          # BaseEntity, AuditLog, ApiResponse, RoleNames, event contracts
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
scripts/                      # legacy Postgres schema scripts for the local-only `postgres`
                               # container in docker-compose.yml; every service now runs
                               # against TiDB, created by each service's own EF migrations
docker-compose.yml
.env.example
```

Each service folder follows the same shape: `Entities/`, `DTOs/`, `Repositories/`,
`Services/`, `Controllers/`, `Data/` (or `Migrations/`).

---

<div align="center">
<img width="100%" src="https://capsule-render.vercel.app/api?type=waving&color=0:22D3EE,100:6366F1&height=100&section=footer" />
</div>

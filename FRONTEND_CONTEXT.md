# Frontend Context — GKMPS School ERP

A condensed reference for building the Angular (or any) frontend against this backend.
Read this instead of the source when starting frontend work; come back to the backend
README only for infrastructure/deployment questions.

## 1. How the frontend should talk to the backend

**Always call the gateway, never a service directly.** The gateway is the only place
CORS, JWT validation, and rate limiting are enforced for browser traffic.

| Environment | Base URL |
|---|---|
| Local (`docker compose up`) | `http://localhost:5100` |
| Everywhere else | wherever the gateway is deployed |

CORS on the gateway currently allows exactly one origin, read from `Cors:AllowedOrigins`
in `Gateway/SchoolERP.Gateway/appsettings.json` (default `http://localhost:4200`, i.e.
`ng serve`'s default port). If your Angular dev server runs on a different port, either
change that setting or set `FRONTEND_URL` in `.env` before `docker compose up` — see
`docker-compose.yml`'s `gateway` service. **A frontend on an unlisted origin will get
its requests blocked by the browser**, not by an API error — if calls silently fail
with no response body, check this first.

## 2. Auth flow

1. `POST {base}/api/auth/register` with `{ email, password, fullName, role }` → returns
   `{ accessToken, refreshToken, accessTokenExpiresAtUtc, userId, email, fullName, role }`.
2. `POST {base}/api/auth/login` with `{ email, password }` → same shape.
3. Store `accessToken` in memory (not localStorage, to limit XSS blast radius) and
   `refreshToken` more durably (localStorage is acceptable given it's opaque and hashed
   server-side; httpOnly cookie is better if you want to invest in it later).
4. Attach every request with `Authorization: Bearer {accessToken}`.
5. `accessToken` expires in 15 minutes. On a 401, call `POST {base}/api/auth/refresh`
   with `{ accessToken, refreshToken }` (yes, both — the expired access token is needed
   to identify whose refresh token it is) → get a new pair back. This is a good fit for
   an Angular `HttpInterceptor` that catches 401s, refreshes once, and retries the
   original request.
6. `POST {base}/api/auth/logout` with `{ refreshToken }` (needs `Authorization` header
   too) revokes that refresh token server-side.

Roles (exact strings, case-sensitive, used in JWT `role` claim and everywhere
`[Authorize(Roles=...)]` appears): `SuperAdmin`, `Principal`, `Admin`, `Teacher`,
`Student`, `Parent`, `Accountant`, `Librarian`. Gate Angular routes/UI on the `role`
claim decoded from the JWT (or from the login response).

## 3. Response envelope

Every endpoint in every service returns the same shape:

```ts
interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}
```

HTTP status still matters (200/201 success, 401/403 auth, 404 not found, 409 conflict
i.e. business-rule violation like a duplicate) — `success`/`errors` inside the body are
for rendering messages, not for branching on outcome.

Paginated list endpoints return `data` shaped as:

```ts
interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
```

Note: the C# side serializes with `System.Text.Json`'s default camelCase policy, so
JSON keys are `camelCase` (`accessToken`, not `AccessToken`) even though the C# source
uses PascalCase — the table above already reflects the wire format.

## 4. Endpoint reference

`[public]` = no token required. Everything else requires `Authorization: Bearer <token>`.
Role lists shown are the *additional* restriction on top of being logged in; no list
means any authenticated user can call it.

### Identity.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/auth/register` `[public]` | — | |
| `POST /api/auth/login` `[public]` | — | |
| `POST /api/auth/refresh` `[public]` | — | |
| `POST /api/auth/logout` | — | |
| `GET /api/users/{id}` | — | |
| `GET /api/users?role=&keyword=&page=&pageSize=` | SuperAdmin, Principal, Admin | |
| `PATCH /api/users/{id}/status?isActive=` | SuperAdmin, Principal, Admin | |

### Student.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/students` | SuperAdmin, Principal, Admin | admit |
| `GET /api/students/{id}` | — | |
| `GET /api/students?classId=&sectionId=&keyword=&page=&pageSize=` | — | |
| `PATCH /api/students/{id}/class` | SuperAdmin, Principal, Admin | body `{classId, sectionId}` |
| `GET /api/students/stats/active-by-class` | SuperAdmin, Principal, Admin, Teacher | Redis-cached 5 min |
| `POST /api/transfer-certificates/students/{studentId}/request` | SuperAdmin, Principal, Admin, Teacher | body `{reason, requestedLeavingDateUtc}` |
| `POST /api/transfer-certificates/{id}/approve` | SuperAdmin, Principal | |
| `GET /api/transfer-certificates/{id}/download` | — | returns `{downloadUrl, expiresInMinutes}` — a 15-min SAS URL |
| `GET /api/transfer-certificates/verify/{code}` `[public]` | — | |

### Staff.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/staff` | SuperAdmin, Principal, Admin | onboard |
| `GET /api/staff/{id}` | — | |
| `GET /api/staff?designation=&keyword=&page=&pageSize=` | — | |
| `POST /api/staff/{staffId}/leave-requests` | — | body `{leaveType, fromDateUtc, toDateUtc, reason}` |
| `POST /api/staff/{staffId}/leave-requests/{leaveRequestId}/decision` | SuperAdmin, Principal, Admin | body `{approve, note}` |

### Attendance.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/attendance` | SuperAdmin, Principal, Admin, Teacher | body `{studentId, classId, sectionId, date, status, arrivalTime}` |
| `GET /api/attendance/class?classId=&sectionId=&date=` | — | |
| `GET /api/attendance/students/{studentId}/percentage?from=&to=` | — | |

### Academic.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/subjects` | SuperAdmin, Principal, Admin | |
| `GET /api/subjects?classId=` | — | |
| `PUT /api/timetables` | SuperAdmin, Principal, Admin | body `{classId, sectionId, slots: [...]}` |
| `GET /api/timetables?classId=&sectionId=` | — | Redis-cached 5 min |
| `POST /api/homework` | SuperAdmin, Principal, Admin, Teacher | |
| `GET /api/homework?classId=&sectionId=` | — | |

### Examination.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/exams` | SuperAdmin, Principal, Admin, Teacher | |
| `GET /api/exams?classId=` | — | |
| `POST /api/exams/{examId}/publish` | SuperAdmin, Principal | two-step: publishes results |
| `GET /api/exams/{examId}/stats` | SuperAdmin, Principal, Admin, Teacher | average + rankings |
| `GET /api/exams/{examId}/students/{studentId}/report-card` | — | returns SAS URL; requires results published |
| `POST /api/exams/{examId}/marks` | SuperAdmin, Principal, Admin, Teacher | |
| `PATCH /api/exams/{examId}/marks/{marksEntryId}` | SuperAdmin, Principal, Admin, Teacher | correction |

### Fee.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/fee-structures` | SuperAdmin, Principal, Admin, Accountant | |
| `GET /api/fee-structures?classId=&academicYear=` | — | |
| `POST /api/fee-payments` | SuperAdmin, Principal, Admin, Accountant | body `{studentId, feeStructureId, amount, paymentMethod, gatewayReference}` |
| `GET /api/fee-payments/transactions/{transactionId}/receipt` | — | returns SAS URL |
| `POST /api/fee-payments/waivers/request` | SuperAdmin, Principal, Admin, Accountant | |
| `POST /api/fee-payments/{feePaymentId}/waivers/approve` | SuperAdmin, Principal | |
| `GET /api/fee-payments/collection-totals?fromUtc=&toUtc=` | SuperAdmin, Principal, Admin, Accountant | |

### Communication.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/announcements` | SuperAdmin, Principal, Admin | |
| `GET /api/announcements?role=&classId=&page=&pageSize=` | — | |
| `POST /api/parent-messages` | — | body `{studentId, recipientUserId, body}` |
| `GET /api/parent-messages/students/{studentId}` | — | thread |
| `POST /api/parent-messages/{messageId}/read` | — | |

### Library.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/books` | SuperAdmin, Principal, Admin, Librarian | |
| `GET /api/books?keyword=&category=&page=&pageSize=` | — | |
| `POST /api/book-issues` | SuperAdmin, Principal, Admin, Librarian | |
| `POST /api/book-issues/return` | SuperAdmin, Principal, Admin, Librarian | |

### Transport.API

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/routes` | SuperAdmin, Principal, Admin | |
| `GET /api/routes` | — | |
| `POST /api/vehicles` | SuperAdmin, Principal, Admin | |
| `POST /api/student-route-mappings` | SuperAdmin, Principal, Admin | |

### Notification.API

| Method & path | Roles | Notes |
|---|---|---|
| `GET /api/notifications?recipientReference=&page=&pageSize=` | SuperAdmin, Principal, Admin | delivery history, not a send endpoint |

### Reporting.API

| Method & path | Roles | Notes |
|---|---|---|
| `GET /api/reports/enrollment` | SuperAdmin, Principal, Admin, Accountant | |
| `GET /api/reports/enrollment/pdf` | SuperAdmin, Principal, Admin, Accountant | binary PDF response |
| `GET /api/reports/fee-collection?fromUtc=&toUtc=` | SuperAdmin, Principal, Admin, Accountant | |

## 5. Things that will trip you up

- **Dates**: request/response DTOs use `DateTime`/`DateOnly`/`TimeOnly` which serialize
  as ISO 8601 strings (`"2026-07-02T00:00:00"`, `"2026-07-02"`, `"14:30:00"`
  respectively). Send the same format back.
- **File downloads**: certificates/receipts/report cards never return a file directly —
  they return `{ downloadUrl, expiresInMinutes: 15 }`. Fetch that URL separately (it
  points at Blob Storage, not the API) and it expires, so don't cache it.
- **Guids**: every ID is a GUID string (`"3fa85f64-5717-4562-b3fc-2c963f66afa6"`), not a
  number.
- **CORS is origin-based, not endpoint-based** — if you add a new frontend
  deployment (staging, a second dev machine), add its origin to `Cors:AllowedOrigins`.

## 6. Current backend limitations to design around

- Only **Identity.API and Student.API** (plus Fee.API) have been confirmed to compile
  in this environment; the other 9 services are believed-correct but unverified by a
  real build — see the backend README's "A note on verification". If something 500s
  unexpectedly, it might be a backend compile issue, not a frontend bug.
- No test data / seed script exists yet. Start every flow from `POST /api/auth/register`.
- `Notification.API`'s dispatch is stubbed to logging only — no real email/SMS goes out
  yet, so don't build UI that assumes a user actually received something.
- CORS was just added and is unverified against a real browser — if you hit a CORS
  error immediately, double check `Cors:AllowedOrigins` matches your dev server's exact
  origin (scheme + host + port).

## 7. Suggested Angular project shape

One feature module per backend service keeps the mapping obvious: `auth/`, `students/`,
`staff/`, `attendance/`, `academic/`, `examinations/`, `fees/`, `communication/`,
`library/`, `transport/`, `notifications/`, `reports/`, plus a `core/` module for the
HTTP interceptor (attach token, handle 401 refresh), the `ApiResponse<T>` wrapper types,
and a `RoleNames` const matching section 2 above so route guards don't hand-type role
strings in multiple places.

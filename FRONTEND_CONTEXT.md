# Frontend Context — GKMPS School ERP

A condensed reference for building the Angular (or any) frontend against this backend.
Read this instead of the source when starting frontend work; come back to the backend
README only for infrastructure/deployment questions.

## 1. How the frontend should talk to the backend

The backend is a single app; every endpoint below lives under the same base URL. CORS,
JWT validation and rate limiting are all enforced there.

| Environment | Base URL |
|---|---|
| Local (`docker compose up`) | `http://localhost:5100` (`ng serve` proxies `/api` to it) |
| Everywhere else | set `apiBaseUrl` in the frontend's `public/config.json` (empty = same origin) |

CORS allows the origins in `Cors:AllowedOrigins` (`src/SchoolERP.Api/appsettings.json`,
default `http://localhost:4200`, i.e. `ng serve`'s default port). To allow another origin,
set `FRONTEND_URL` in `.env` before `docker compose up` (see `docker-compose.yml`'s `app`
service). **A frontend on an unlisted origin will get
its requests blocked by the browser**, not by an API error — if calls silently fail
with no response body, check this first.

## 2. Auth flow

1. `POST {base}/api/auth/register` with `{ email, username?, password, fullName, role }`
   (owner/principal/admin only, and only for roles below the caller's own — see below) →
   returns `{ accessToken, refreshToken, accessTokenExpiresAtUtc, userId, email, fullName, role }`.
2. `POST {base}/api/auth/login` with `{ loginId, password }` (login ID or email) → same shape.
3. Store `accessToken` in memory (not localStorage, to limit XSS blast radius) and
   `refreshToken` more durably (localStorage is acceptable given it's opaque and hashed
   server-side; httpOnly cookie is better if you want to invest in it later).
4. Attach every request with `Authorization: Bearer {accessToken}`.
5. `accessToken` expires in 15 minutes. On a 401, call `POST {base}/api/auth/refresh`
   with `{ refreshToken, accessToken? }` → get a new pair back. The access token is
   optional (after a page reload the in-memory one is gone); if you do send it, it must
   belong to the same user. This is a good fit for an Angular `HttpInterceptor` that
   catches 401s, refreshes once, and retries the original request.
6. `POST {base}/api/auth/logout` with `{ refreshToken }` ends that session server-side.
   No `Authorization` header needed.
7. `POST {base}/api/auth/change-password` with `{ currentPassword, newPassword }`
   (signed in) → a fresh token pair; every other session is ended.
8. **Sessions and inactivity.** Every sign-in is a server-side session; the access token
   carries its id (`sid`). Login/refresh responses include `sessionIdleTimeoutMinutes`
   (default 30). The server treats each signed-in request as activity and ends the session
   after that many minutes without any (+5 minutes' grace), after which requests get 401 and
   refresh fails with a message saying why. The frontend (`IdleService`) tracks real user
   activity — mouse, keyboard, touch, scrolling — shared across tabs, warns a minute before,
   then signs out; while the user is active but not calling the API it sends
   `POST {base}/api/sessions/heartbeat` (signed in, at most every 2 minutes).

Account management is hierarchical: the owner (`SuperAdmin`) can create and administer
any account; a `Principal` only `Admin` and below; an `Admin` only non-admin roles.
Nobody can deactivate their own account.

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

### Identity

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/auth/register` | SuperAdmin, Principal, Admin | only roles below the caller's |
| `POST /api/auth/login` `[public]` | — | |
| `POST /api/auth/refresh` `[public]` | — | body `{refreshToken, accessToken?}` |
| `POST /api/auth/logout` `[public]` | — | body `{refreshToken}` |
| `POST /api/auth/change-password` | — | body `{currentPassword, newPassword}` |
| `POST /api/sessions/heartbeat` | — | keeps the session open while the user is active; returns `{idleTimeoutMinutes}` |
| `GET /api/users/{id}` | — | admins: anyone; others: only themselves |
| `GET /api/users?role=&keyword=&page=&pageSize=` | SuperAdmin, Principal, Admin | keyword matches name, email, login ID |
| `PATCH /api/users/{id}/status?isActive=` | SuperAdmin, Principal, Admin | lower-ranked accounts only |
| `PATCH /api/users/{id}/password` | SuperAdmin, Principal, Admin | lower-ranked accounts only |
| `DELETE /api/users/{id}` | SuperAdmin, Principal, Admin | never-used accounts only (onboarding rollback) |

### Student

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/students` | SuperAdmin, Principal, Admin | admit |
| `GET /api/students/{id}` | — | |
| `GET /api/students?classId=&sectionId=&keyword=&page=&pageSize=` | SuperAdmin, Principal, Admin, Teacher, Accountant, Librarian | teachers are scoped to the class they head |
| `PATCH /api/students/{id}/class` | SuperAdmin, Principal, Admin | body `{classId, sectionId}` |
| `PATCH /api/students/{id}/parent-account` | SuperAdmin, Principal, Admin | body `{parentUserId}` (null unlinks); the Parent login then gets this student's claims |
| `GET /api/students/stats/active-by-class` | SuperAdmin, Principal, Admin, Teacher, Accountant | cached 5 min |
| `POST /api/transfer-certificates/students/{studentId}/request` | SuperAdmin, Principal, Admin, Teacher | body `{reason, requestedLeavingDateUtc}` |
| `POST /api/transfer-certificates/{id}/approve` | SuperAdmin, Principal | |
| `GET /api/transfer-certificates/{id}/download` | — | returns `{downloadUrl, expiresInMinutes}` — a 15-min signed link; students/parents only their own |
| `GET /api/transfer-certificates/verify/{code}` `[public]` | — | |

### Staff

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/staff` | SuperAdmin, Principal, Admin | onboard |
| `GET /api/staff/{id}` | — | salary/phone/email only for admins, the accountant and the person themselves |
| `GET /api/staff?designation=&keyword=&page=&pageSize=` | SuperAdmin, Principal, Admin, Accountant | |
| `POST /api/staff/{staffId}/leave-requests` | — | yourself only (admins for anyone); body `{leaveType, fromDateUtc, toDateUtc, reason}` |
| `POST /api/staff/{staffId}/leave-requests/{leaveRequestId}/decision` | SuperAdmin, Principal, Admin | body `{approve, note}` |

### Attendance

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/attendance` | SuperAdmin, Principal, Admin, Teacher | body `{studentId, classId, sectionId, date, status, arrivalTime}` |
| `GET /api/attendance/class?classId=&sectionId=&date=` | — | |
| `GET /api/attendance/students/{studentId}/percentage?from=&to=` | — | |

### Academic

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/subjects` | SuperAdmin, Principal, Admin | |
| `GET /api/subjects?classId=` | — | classId optional for staff; students/parents only their own class |
| `PUT /api/timetables` | SuperAdmin, Principal, Admin | body `{classId, sectionId, slots: [...]}` |
| `GET /api/timetables?classId=&sectionId=` | — | cached 5 min |
| `POST /api/homework` | SuperAdmin, Principal, Admin, Teacher | |
| `GET /api/homework?classId=&sectionId=` | — | |

### Examination

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/exams` | SuperAdmin, Principal, Admin, Teacher | |
| `GET /api/exams?classId=` | — | classId optional for staff; students/parents only their own class |
| `POST /api/exams/{examId}/publish` | SuperAdmin, Principal | two-step: publishes results |
| `GET /api/exams/{examId}/stats` | SuperAdmin, Principal, Admin, Teacher | average + rankings |
| `GET /api/exams/{examId}/students/{studentId}/report-card` | — | returns a signed link; requires results published |
| `POST /api/exams/{examId}/marks` | SuperAdmin, Principal, Admin, Teacher | |
| `PATCH /api/exams/{examId}/marks/{marksEntryId}` | SuperAdmin, Principal, Admin, Teacher | correction |

### Fee

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/fee-structures` | SuperAdmin, Principal, Admin, Accountant | |
| `GET /api/fee-structures?classId=&academicYear=` | — | both optional for staff; students/parents only their own class |
| `POST /api/fee-payments` | SuperAdmin, Principal, Admin, Accountant | body `{studentId, feeStructureId, amount, paymentMethod, gatewayReference}` |
| `GET /api/fee-payments/transactions/{transactionId}/receipt` | — | returns a signed link; students/parents only their own |
| `POST /api/fee-payments/waivers/request` | SuperAdmin, Principal, Admin, Accountant | |
| `POST /api/fee-payments/{feePaymentId}/waivers/approve` | SuperAdmin, Principal | |
| `GET /api/fee-payments/collection-totals?fromUtc=&toUtc=` | SuperAdmin, Principal, Admin, Accountant | |

### Communication

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/announcements` | SuperAdmin, Principal, Admin | |
| `GET /api/announcements?role=&classId=&page=&pageSize=` | — | role/classId honoured for admins only; everyone else is scoped from their token. Returns a bare array |
| `POST /api/parent-messages` | SuperAdmin, Principal, Admin, Teacher, Parent (own child) | body `{studentId, recipientUserId, body}` |
| `GET /api/parent-messages/students/{studentId}` | same | thread; non-admins see only messages they sent or received |
| `POST /api/parent-messages/{messageId}/read` | — | recipient only |

### Library

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/books` | SuperAdmin, Principal, Admin, Librarian | |
| `GET /api/books?keyword=&category=&page=&pageSize=` | — | returns a bare array |
| `GET /api/book-issues?activeOnly=&studentId=` | SuperAdmin, Principal, Admin, Librarian | recent issues with book title and `isOverdue` |
| `POST /api/book-issues` | SuperAdmin, Principal, Admin, Librarian | body `{bookId, studentId, dueDateUtc}` |
| `POST /api/book-issues/return` | SuperAdmin, Principal, Admin, Librarian | body `{issueId}` |

### Transport

| Method & path | Roles | Notes |
|---|---|---|
| `POST /api/routes` | SuperAdmin, Principal, Admin | |
| `GET /api/routes` | — | |
| `POST /api/vehicles` | SuperAdmin, Principal, Admin | |
| `POST /api/student-route-mappings` | SuperAdmin, Principal, Admin | |

### Notification

| Method & path | Roles | Notes |
|---|---|---|
| `GET /api/notifications?recipientReference=&page=&pageSize=` | SuperAdmin, Principal, Admin | delivery history, not a send endpoint |

### Reporting

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
  they return `{ downloadUrl, expiresInMinutes: 15 }`. Open that URL separately (no auth
  header needed — the link itself is signed) and it expires, so don't cache it. With the
  default database file store it's relative (`/api/files/...`), so prefix the API base URL;
  with Azure Blob it's an absolute SAS URL. `openDownload()` in `runtime-config.ts` handles both.
- **Guids**: every ID is a GUID string (`"3fa85f64-5717-4562-b3fc-2c963f66afa6"`), not a
  number.
- **CORS is origin-based, not endpoint-based** — if you add a new frontend
  deployment (staging, a second dev machine), add its origin to `Cors:AllowedOrigins`.

## 6. Current backend limitations to design around

- No test data / seed script exists. Sign in as the seeded owner and create accounts from there.
- Notifications send real email only when an SMTP relay is configured (`SMTP_*`
  in `.env`); SMS and push have no provider yet. Don't build UI that assumes a user
  actually received something.
- If you hit a CORS error, check that `Cors:AllowedOrigins` matches your dev server's exact
  origin (scheme + host + port).

## 7. Suggested Angular project shape

One feature module per backend module keeps the mapping obvious: `auth/`, `students/`,
`staff/`, `attendance/`, `academic/`, `examinations/`, `fees/`, `communication/`,
`library/`, `transport/`, `notifications/`, `reports/`, plus a `core/` module for the
HTTP interceptor (attach token, handle 401 refresh), the `ApiResponse<T>` wrapper types,
and a `RoleNames` const matching section 2 above so route guards don't hand-type role
strings in multiple places.

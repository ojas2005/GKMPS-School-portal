# Role-Based Data Scoping — what changed & how to rebuild

This adds **server-enforced per-student data scoping**: a Student/Parent can only read
their own records, enforced in the backend (not just hidden in the UI).

## The mechanism

1. **Identity puts scope claims in the token.** At login/refresh, if the account's role
   is `Student`, Identity looks up the linked student record and embeds `studentId`,
   `classId`, `sectionId` claims in the JWT (and returns them in the login response).
   - New file: `Services/Identity.API/Auth/StudentProfileResolver.cs` (reads the student
     record; best-effort — never blocks login).
   - Edited: `TokenGenerator`, `AuthService`, `AuthDtos.AuthResult`, `Program.cs`.
2. **A shared helper enforces ownership.** `BuildingBlocks/SchoolERP.Shared/Common/CallerClaims.cs`
   exposes `User.CanAccessStudent(id)` / `User.CanAccessClass(classId)` — they pass for
   staff/teacher and pass for a self-service caller only on their own id/class.
3. **Read endpoints are scoped.** Each service rejects cross-student/cross-class reads:
   - **Student**: `GET /api/students` (the directory) is now staff/teacher only;
     `GET /api/students/{id}` returns `403` for a student reading someone else.
   - **Attendance**: `GET /students/{id}/percentage` scoped; the full class register is
     staff/teacher only.
   - **Fee**: new `GET /api/fee-payments/students/{studentId}` (a student's own ledger),
     scoped.
   - **Examination**: report-card endpoint scoped.
   - **Academic**: timetable & homework reads scoped to the caller's own class.

Staff roles are unchanged — they keep full access via the existing role attributes.

## Rebuild (required — backend code changed)

```bash
cd "GKMPS School portal"
docker compose up --build -d
```

No new migration is needed (no schema change). If you want a clean DB:
`docker compose down -v && docker compose up --build`.

Then re-run the seed if you want fresh demo data (`node seed.mjs`), and on the frontend
just refresh — no rebuild needed there (already done).

## How to test per role

1. **Create a student with a known login.** In the app as an admin, go to Students →
   **+ Admit Student**, set **Login email** (e.g. `aarav.student@gkmps.test`), pick a
   class, Admit. The account password is **`Password@123`**.
2. **Log out, log in as that student.** You should see a reduced menu (no Teachers /
   Reports / Settings), a dashboard without admin stats, **My Fees** (only their ledger),
   **My Attendance** (their %), and Academics limited to their class.
3. **Try to break it (should fail):** while logged in as the student, in the browser
   console:
   ```js
   fetch('/api/students').then(r => console.log(r.status))            // 403
   fetch('/api/fee-payments/students/<someone-elses-id>').then(r=>console.log(r.status)) // 403
   ```
   A staff login hitting the same URLs returns `200`.

## Honest caveats

- **I could not compile the backend here** (no .NET/Docker in my environment), so the C#
  is careful but unverified by a build. Rebuild and share any compile errors — they'll be
  quick to fix.
- **Parent scoping is partial.** The data model has no parent→student link (a Student row
  stores a parent's *email*, not a parent user id), so a Parent account gets no student
  claims and self-service pages will say "not linked yet." Wiring real parent access needs
  a parent→student relationship added to the backend.
- A student must be **admitted** (not just registered) to get scope claims — the claims
  come from the student record, which admission creates. If you admit a student *after*
  they're already logged in, they need to log in again to pick up the claims.

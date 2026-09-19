# What the portal keeps, where, and for how long

For the school's privacy notice and for India's DPDP Act 2023 / DPDP Rules 2025. Update it
whenever a feature starts storing something new.

## Where the data lives

| What | Service | Region |
|---|---|---|
| All school records (the 12 module databases), generated PDFs (`files`), audit trail (`audit`) | TiDB Cloud Starter | AWS Tokyo, Japan |
| The API | Azure Container Apps `gkmps-api` | Korea Central |
| The web pages (no personal data) | Azure Static Web Apps `gkmps-portal` | East Asia |
| API logs (no request bodies) | Azure Log Analytics | Korea Central |
| Nightly encrypted backups | GitHub Actions artifacts | GitHub (US) |

The data is stored outside India. The DPDP Act allows this except to countries the government
restricts; none of these are restricted today. The privacy notice says so.

## How long each thing is kept

| Record | Kept for | How it goes |
|---|---|---|
| Student profile, marks, attendance | While the student is enrolled, and after they leave until the school erases it | Principal / SuperAdmin: student page → **Erase personal data** (only once the transfer certificate is approved). Name, date of birth, gender, address and parent contacts are removed; the student's login and the parent's (unless a sibling still uses it) are closed; transfer certificates and report cards are deleted. Marks and attendance stay under the admission number only. |
| Fee and payment records, receipts | Not deleted automatically — accounts must be kept for years (typically 8) | Kept through erasure; receipts show the student ID, not the name. |
| Staff records and logins | While employed | Deactivate the login when they leave. There is no staff erasure button yet; remove the record by hand on request. |
| Sign-in sessions and refresh tokens (IP address, times) | 30 days after the session ends (`Retention:SessionDays`) | Deleted daily by `RetentionCleanup`. |
| Audit trail (who did what, from which IP) | 400 days (`Audit:RetentionDays`, never under 365 — the DPDP Rules ask for a year) | Deleted on each start by `AuditWriter`. |
| Notification delivery records | 400 days (`Retention:NotificationDays`) | Deleted daily by `RetentionCleanup`; removed at once on erasure. |
| API logs in Azure | 30 days | Log Analytics workspace retention. |
| Nightly backups | 90 days | GitHub deletes the artifact. An erased student's data can remain in backups up to 90 days. |
| TiDB Cloud's own backup | 1 day | TiDB Cloud. |

## Requests from families

- **See or correct their data:** the parent sees it on the portal; corrections go through the
  school office, which edits the student record.
- **Erase:** once the child has left, the principal uses **Erase personal data**. Before that, the
  school needs the record to teach and assess the child. Children's data for educational
  purposes doesn't need separate verifiable parental consent under the DPDP Rules, but the
  privacy notice must still explain what is kept and why.
- **Complaints:** to the grievance contact in the privacy notice. They must be answered within
  90 days; after that the family can go to the Data Protection Board.

## Breaches

See [INCIDENT-RESPONSE.md](INCIDENT-RESPONSE.md).

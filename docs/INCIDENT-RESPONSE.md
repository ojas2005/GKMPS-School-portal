# Personal data breach: what to do

The portal holds children's personal data, so India's Digital Personal Data Protection Act 2023
and the DPDP Rules 2025 apply. For a breach the Rules require the school (the "data fiduciary")
to:

- tell **every affected person** (for a child, their parent) **without delay**, and
- tell the **Data Protection Board of India** at once, followed by a full report **within 72 hours**
  of becoming aware of it.

This page is the checklist. Keep it short in the moment; fill in the log as you go.

## Who does what

| Role | Person | Contact |
|---|---|---|
| Incident lead (decides, speaks for the school) | Principal | _fill in_ |
| Technical lead (contains, investigates) | Portal maintainer | _fill in_ |
| Grievance / privacy contact (named in the privacy notice) | _fill in_ | _fill in_ |

## 1. First hour: contain

Signs: data you didn't publish turns up elsewhere; a user sees another family's records;
logins nobody recognises in the audit trail; a leaked password, key or backup passphrase.

1. **Write down the time you found out.** The 72 hours run from here.
2. Stop the leak, in order of how quickly it can be done:
   - **A stolen or shared login:** Settings → Users → deactivate it. This ends every session of
     that account immediately.
   - **Many logins or the admin accounts:** rotate the JWT signing key (Azure → `gkmps-api` →
     Secrets → `jwt-signing-key`, then roll a new revision by redeploying the image).
     Everyone is signed out and every download link stops working. Two-step sign-in is not
     affected: it has its own key (`two-factor-key`). Rotate that one too only if it may have
     leaked — people then sign in with a recovery code and set up their phone again.
   - **Database password leaked:** TiDB Cloud console → reset the password, update the
     `school-db` secret on `gkmps-api`, roll a new revision.
   - **Backup passphrase leaked:** set a new `BACKUP_PASSPHRASE` GitHub secret; delete the old
     backup artifacts in GitHub Actions (they are readable with the old passphrase).
   - **The whole site must stop:** `az containerapp update -g gkmps-prod -n gkmps-api --max-replicas 0`
     (undo with `--max-replicas 1`).
3. **Keep the evidence.** Don't delete accounts or data. Export the audit trail for the period
   (`GET /api/audit?fromUtc=…&toUtc=…&pageSize=100`, signed in as SuperAdmin or Principal) and
   save it offline. The audit trail keeps 400 days; the Azure container logs only 30.

## 2. Within 24 hours: understand

Answer, in the incident log below:

- What data: which people (students, parents, staff), which fields (names, dates of birth,
  contacts, marks, fees, certificates)?
- How many people?
- How it happened, and when it started (the audit trail shows every sign-in, change and file
  download with the account and IP address).
- Is it stopped?

## 3. Tell people: without delay, and the Board within 72 hours

**Board:** use the breach intimation facility on the Data Protection Board's online
portal (check the current link on meity.gov.in). The first notice can be short; the detailed report is due within 72 hours
of the time noted in step 1.

**Affected families and staff:** in plain language, in English and Hindi. The Rules ask for:
what happened, when, what data, the likely consequences, what the school has done, what they
should do, and who to contact.

> The portal cannot send email or SMS yet (no mail provider is configured), so notices go out
> through the school's usual channels — a letter, the class WhatsApp groups and a notice on the
> portal's announcements. Setting `Smtp__Host`, `Smtp__Username`, `Smtp__Password` and
> `Smtp__From` on the API would let the portal email them directly.

Template:

> **Subject: Important: a security incident affecting your child's school records**
>
> On _date_, GKMPS found that _what happened, one sentence_. The information involved was
> _fields_ for _whose records_. _It was / We have no evidence it was_ used by anyone.
>
> We have _what was done: blocked the account, changed passwords, closed the gap_ and reported
> the incident to the Data Protection Board of India.
>
> What you can do: _e.g. change your portal password; be careful with calls or messages that
> mention your child's details and ask for money or passwords — the school never asks for these_.
>
> For questions contact _grievance contact, phone, email_. We are sorry this happened.

## 4. Afterwards

- Fix the cause, and add a test so it can't come back quietly.
- Rotate anything that might still be exposed.
- Record what happened, what was done and when in the log below. Keep it for at least a year.

## Incident log

| Found (date, time) | What | People affected | Contained (time) | Board told (time) | Families told (time) | Cause and fix |
|---|---|---|---|---|---|---|
| | | | | | | |

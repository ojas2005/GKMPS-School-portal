// GKMPS School ERP — API seed script.
// Populates the running backend with demo data by calling the gateway (no SQL).
// Run the backend first (docker compose up), then:  node seed.mjs
// Optional: API=http://localhost:5100 node seed.mjs
//
// Requires Node 18+ (uses global fetch). You have Node 22, so `node seed.mjs` works.

const BASE = process.env.API || 'http://localhost:5100';

// ---- tweak these counts to taste ----
const NUM_STUDENTS = 12;
const NUM_TEACHERS = 4;

// Stable class/section IDs — these MATCH the frontend catalog in
// src/app/core/constants/classes.ts, so seeded data lines up with the UI dropdowns.
const SEC_A = '5ec00000-0000-4000-8000-00000000000a';
const SEC_B = '5ec00000-0000-4000-8000-00000000000b';
const CLASSES = [
  { id: 'c1a55014-0000-4000-8000-000000000014', name: 'Class 10', sectionId: SEC_A, sectionName: 'A' },
  { id: 'c1a55013-0000-4000-8000-000000000013', name: 'Class 9',  sectionId: SEC_B, sectionName: 'B' },
];

const RUN = Date.now().toString(36); // unique suffix so sample emails never collide
const PASSWORD = 'Password@123';     // backend requires >= 8 chars

const FIRST = ['Aarav', 'Diya', 'Vivaan', 'Ananya', 'Aditya', 'Ishaan', 'Saanvi', 'Kabir', 'Myra', 'Reyansh', 'Anika', 'Arjun', 'Kiara', 'Vihaan', 'Riya'];
const LAST = ['Sharma', 'Verma', 'Gupta', 'Iyer', 'Nair', 'Reddy', 'Khan', 'Bose', 'Mehta', 'Patel'];
const pick = (a, i) => a[i % a.length];

let token = null;
const log = (...a) => console.log(...a);
const warn = (m) => console.warn('  ! ' + m);

async function call(method, path, body) {
  const res = await fetch(`${BASE}${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  let json = null;
  try { json = await res.json(); } catch { /* empty body */ }
  const data = json && json.data !== undefined ? json.data : json;
  return { ok: res.ok, status: res.status, data, raw: json };
}

async function ensureAdmin() {
  // Registration is closed to the public; everything is created under the seeded
  // school-owner account (Identity.API seeds it on startup).
  const loginId = process.env.OWNER_USER || 'ownerishim';
  const pwd = process.env.OWNER_PASS || 'Owner@1234';
  const r = await call('POST', '/api/auth/login', { loginId, password: pwd });
  if (!r.ok || !r.data?.accessToken) {
    throw new Error(`Could not log in as the owner '${loginId}' (status ${r.status}). Is the backend up on ${BASE}? ` + JSON.stringify(r.raw));
  }
  token = r.data.accessToken;
  log(`\n✔ Owner ready — login with:  ${loginId} / ${pwd}\n`);
}

async function registerUser(fullName, role, tag) {
  const email = `${tag}.${RUN}@gkmps.test`;
  const username = `${tag}.${RUN}`;
  const r = await call('POST', '/api/auth/register', { email, username, password: PASSWORD, fullName, role });
  if (!r.ok || !r.data?.userId) { warn(`register ${role} "${fullName}" failed (${r.status})`); return null; }
  return { userId: r.data.userId, email, username };
}

async function seedTeachers() {
  log('Seeding teachers/staff…');
  const staff = [];
  for (let i = 0; i < NUM_TEACHERS; i++) {
    const name = `${pick(FIRST, i + 3)} ${pick(LAST, i)}`;
    const u = await registerUser(name, 'Teacher', `teacher${i + 1}`);
    if (!u) continue;
    // The first two teachers become class teachers (head teachers) of the two demo
    // classes — only they can upload their class's attendance.
    const cls = i < CLASSES.length ? CLASSES[i] : null;
    const r = await call('POST', '/api/staff', {
      linkedUserId: u.userId,
      employeeCode: `EMP-${RUN}-${i + 1}`,
      fullName: name,
      designation: 'Teacher',
      subjectsTaughtCsv: 'Mathematics,Science',
      phone: `98765${(10000 + i).toString().slice(-5)}`,
      email: u.email,
      classTeacherOfClassId: cls?.id,
      classTeacherOfSectionId: cls?.sectionId,
    });
    if (r.ok) {
      staff.push({ id: r.data?.id, name, username: u.username });
      log(`  + teacher ${name} (login: ${u.username} / ${PASSWORD})${cls ? ` — class teacher of ${cls.name}-${cls.sectionName}` : ''}`);
    }
    else warn(`onboard ${name} failed (${r.status})`);
  }

  // A couple of payout entries so the teacher portal has data.
  for (const s of staff.slice(0, 2)) {
    if (!s.id) continue;
    for (const period of ['May 2026', 'June 2026']) {
      const r = await call('POST', '/api/payouts', {
        staffId: s.id, amount: 32000, periodLabel: period, method: 'BankTransfer', reference: `SAL-${RUN}`,
      });
      if (!r.ok) warn(`payout ${period} for ${s.name} failed (${r.status})`);
    }
  }
  return staff;
}

async function seedStudents() {
  log('Seeding students…');
  const students = [];
  for (let i = 0; i < NUM_STUDENTS; i++) {
    const name = `${pick(FIRST, i)} ${pick(LAST, i + 2)}`;
    const cls = CLASSES[i % CLASSES.length];
    const u = await registerUser(name, 'Student', `student${i + 1}`);
    if (!u) continue;
    const r = await call('POST', '/api/students', {
      linkedUserId: u.userId,
      admissionNumber: `ADM-${RUN}-${(1000 + i)}`,
      fullName: name,
      dateOfBirth: `20${10 + (i % 5)}-0${1 + (i % 8)}-15T00:00:00Z`,
      gender: i % 2 ? 'Female' : 'Male',
      classId: cls.id,
      sectionId: cls.sectionId,
      parentName: `${pick(LAST, i + 2)} (Parent)`,
      parentEmail: `parent${i + 1}.${RUN}@gkmps.test`,
      parentPhone: `99887${(10000 + i).toString().slice(-5)}`,
      address: `${i + 1} MG Road, City`,
    });
    if (r.ok) { students.push({ id: r.data?.id, name, cls }); log(`  + student ${name} (${cls.name}-${cls.sectionName}, login: ${u.username} / ${PASSWORD})`); }
    else warn(`admit ${name} failed (${r.status}) ${JSON.stringify(r.raw)}`);
  }
  return students;
}

async function seedSubjects(staff) {
  log('Seeding subjects…');
  const subjects = [];
  const defs = [
    { code: 'MATH', name: 'Mathematics' },
    { code: 'SCI', name: 'Science' },
    { code: 'ENG', name: 'English' },
  ];
  for (const cls of CLASSES) {
    for (const d of defs) {
      const r = await call('POST', '/api/subjects', {
        code: `${d.code}-${cls.name.replace(/\s/g, '')}`,
        name: d.name,
        classId: cls.id,
        teacherStaffId: staff[0]?.id,
      });
      if (r.ok) { subjects.push({ id: r.data?.id, name: d.name, classId: cls.id }); }
      else warn(`subject ${d.name}/${cls.name} failed (${r.status})`);
    }
  }
  log(`  + ${subjects.length} subjects`);
  return subjects;
}

async function seedFees() {
  log('Seeding fee structures…');
  for (const cls of CLASSES) {
    const r = await call('POST', '/api/fee-structures', {
      classId: cls.id,
      name: `${cls.name} Tuition (Term 1)`,
      amount: 25000 + CLASSES.indexOf(cls) * 5000,
      academicYear: '2026-27',
      dueDateUtc: '2026-08-31T00:00:00Z',
    });
    log(r.ok ? `  + fee structure for ${cls.name}` : `  ! fee ${cls.name} failed (${r.status})`);
  }
}

async function seedAnnouncements() {
  log('Seeding announcements…');
  const items = [
    { title: 'Welcome to the 2026-27 session', body: 'Classes resume Monday. Please check your timetable.' },
    { title: 'PTM scheduled', body: 'Parent-teacher meeting on the 15th at 10:00 AM in the main hall.' },
    { title: 'Sports Day', body: 'Annual sports day trials begin next week. Sign up with your class teacher.' },
  ];
  for (const a of items) {
    const r = await call('POST', '/api/announcements', { title: a.title, body: a.body, targetRolesCsv: 'Student,Parent' });
    log(r.ok ? `  + "${a.title}"` : `  ! announcement failed (${r.status})`);
  }
}

async function seedBooks() {
  log('Seeding library books…');
  const books = [
    { isbn: '9780140328721', title: 'Matilda', author: 'Roald Dahl', category: 'Fiction', totalCopies: 5 },
    { isbn: '9780439554930', title: "Harry Potter and the Sorcerer's Stone", author: 'J.K. Rowling', category: 'Fiction', totalCopies: 8 },
    { isbn: '9788173711461', title: 'NCERT Mathematics X', author: 'NCERT', category: 'Textbook', totalCopies: 30 },
    { isbn: '9788126415045', title: 'A Brief History of Time', author: 'Stephen Hawking', category: 'Science', totalCopies: 3 },
  ];
  for (const b of books) {
    const r = await call('POST', '/api/books', b);
    log(r.ok ? `  + ${b.title}` : `  ! book ${b.title} failed (${r.status})`);
  }
}

async function seedRoutes() {
  log('Seeding transport routes…');
  const routes = [
    { name: 'Route 1 — North', startPoint: 'Depot', endPoint: 'Sector 15', monthlyFee: 1500 },
    { name: 'Route 2 — East', startPoint: 'Depot', endPoint: 'Lake Town', monthlyFee: 1800 },
  ];
  for (const rt of routes) {
    const r = await call('POST', '/api/routes', rt);
    log(r.ok ? `  + ${rt.name}` : `  ! route ${rt.name} failed (${r.status})`);
  }
}

async function seedExams(subjects) {
  log('Seeding exams…');
  for (const cls of CLASSES) {
    const subj = subjects.find((s) => s.classId === cls.id);
    if (!subj?.id) { warn(`no subject for ${cls.name}, skipping exam`); continue; }
    const r = await call('POST', '/api/exams', {
      name: `${cls.name} — Unit Test 1`,
      classId: cls.id,
      subjectId: subj.id,
      examDateUtc: '2026-08-10T09:00:00Z',
      maxMarks: 100,
      passingMarks: 35,
    });
    log(r.ok ? `  + exam for ${cls.name}` : `  ! exam ${cls.name} failed (${r.status})`);
  }
}

async function seedAttendance(students) {
  log('Marking today\'s attendance…');
  const today = new Date().toISOString().slice(0, 10);
  let ok = 0;
  for (const s of students.slice(0, 8)) {
    if (!s.id) continue;
    const r = await call('POST', '/api/attendance', {
      studentId: s.id, classId: s.cls.id, sectionId: s.cls.sectionId,
      date: today, status: 'Present', arrivalTime: '08:45:00',
    });
    if (r.ok) ok++;
  }
  log(`  + marked ${ok} students present`);
}

(async () => {
  log(`\nSeeding GKMPS backend at ${BASE}`);
  try {
    await ensureAdmin();
    const staff = await seedTeachers();
    const students = await seedStudents();
    const subjects = await seedSubjects(staff);
    await seedFees();
    await seedAnnouncements();
    await seedBooks();
    await seedRoutes();
    await seedExams(subjects);
    await seedAttendance(students);
    log('\n✅ Seeding complete. Log in at the app as the owner:  ownerishim / Owner@1234');
    log('   (all seeded teacher/student logins are printed above; their password is ' + PASSWORD + ')\n');
  } catch (e) {
    console.error('\n❌ Seeding aborted:', e.message, '\n');
    process.exit(1);
  }
})();

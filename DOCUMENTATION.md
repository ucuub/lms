# LMS App — Dokumentasi Teknis Lengkap

> **Stack:** ASP.NET Core 8 MVC · Entity Framework Core · PostgreSQL · Keycloak OIDC
> **Repo:** `C:\Users\yusuf4\Desktop\lms-new`
> **Project:** `src/LmsApp/LmsApp.csproj`

---

## Daftar Isi

1. [Arsitektur Sistem](#1-arsitektur-sistem)
2. [Instalasi & Konfigurasi](#2-instalasi--konfigurasi)
3. [Autentikasi & Otorisasi](#3-autentikasi--otorisasi)
4. [Database & Model](#4-database--model)
5. [Controllers & Endpoints](#5-controllers--endpoints)
6. [Views](#6-views)
7. [Services](#7-services)
8. [CSS & JS (Frontend)](#8-css--js-frontend)
9. [Fitur Lengkap](#9-fitur-lengkap)
10. [Changelog per Sesi](#10-changelog-per-sesi)

---

## 1. Arsitektur Sistem

```
┌─────────────────────────────────────────────────────────┐
│                        Browser                          │
└────────────────────────┬────────────────────────────────┘
                         │ HTTPS
┌────────────────────────▼────────────────────────────────┐
│              ASP.NET Core 8 MVC (LmsApp)                │
│                                                         │
│  Controllers ──► Views (Razor)                          │
│       │                                                 │
│  Services (FileUpload, Notification, UserSync, Video)   │
│       │                                                 │
│  EF Core DbContext (LmsDbContext)                        │
│       │                                                 │
│  PostgreSQL Database                                    │
└──────────────────────────────────────────────────────── ┘
                         │ OIDC
┌────────────────────────▼────────────────────────────────┐
│              Keycloak (Identity Provider)               │
│         Realm: lms  ·  Client: lms-app                  │
└─────────────────────────────────────────────────────────┘
```

### Alur Request
1. Browser mengirim request → ASP.NET middleware
2. `UseAuthentication` → cek cookie/OIDC token
3. `IClaimsTransformation` → inject role dari tabel `AppUsers` ke claims
4. Controller action dieksekusi
5. Razor view di-render dan dikembalikan ke browser

---

## 2. Instalasi & Konfigurasi

### Prerequisites
- .NET 8 SDK
- PostgreSQL 14+
- Keycloak 21+

### Konfigurasi `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lms;Username=lmsuser;Password=yourpassword"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/lms",
    "ClientId":  "lms-app",
    "ClientSecret": "your-client-secret"
  }
}
```

### Setup Keycloak
1. Buat Realm baru: `lms`
2. Buat Client: `lms-app`
   - Client authentication: **ON**
   - Valid redirect URIs: `https://yourhost/*`
   - Valid post logout redirect URIs: `https://yourhost/*`
3. Scope yang dibutuhkan: `openid`, `profile`, `email`, `roles`

### Jalankan Aplikasi

```bash
cd src/LmsApp
dotnet run
```

Schema database dibuat otomatis saat startup via `EnsureCreated()`.

### Deployment (Coolify / Docker)

```bash
docker build -t lms-app .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Keycloak__Authority="..." \
  -e Keycloak__ClientId="lms-app" \
  -e Keycloak__ClientSecret="..." \
  lms-app
```

---

## 3. Autentikasi & Otorisasi

### Alur Login
```
Browser → GET /Account/Login
       → Redirect ke Keycloak
       → User login di Keycloak
       → Callback dengan authorization code
       → Cookie auth dibuat
       → GET /Account/PostLogin (sync user ke DB)
       → Redirect ke Home
```

### Role System

Role disimpan di tabel `AppUsers` (bukan di Keycloak). Saat setiap request, `AppUserClaimsTransformation` menyuntikkan role dari DB ke `ClaimsPrincipal`.

| Role | Hak Akses |
|------|-----------|
| `student` | Enroll kursus, lihat modul, kerjakan quiz/tugas, tulis review, forum |
| `instructor` | + Buat/edit kursus sendiri, kelola modul/tugas/quiz, lihat analytics kursus |
| `admin` | + Semua akses, kelola semua user, lihat platform analytics |

### Files Terkait
| File | Fungsi |
|------|--------|
| `Services/AppUserClaimsTransformation.cs` | Inject role dari DB ke claims |
| `Services/UserSyncService.cs` | Daftarkan/update AppUser saat login |
| `Controllers/AccountController.cs` | Login, logout, PostLogin sync |
| `Controllers/AdminController.cs` | Kelola role & status user |

### Cara Cek Role di View
```razor
@if (User.IsInRole("admin")) { ... }
@if (User.IsInRole("instructor")) { ... }
```

### Cara Cek Role di Controller
```csharp
[Authorize(Roles = "instructor,admin")]
public IActionResult Create() => View();
```

---

## 4. Database & Model

### Entity Relationship Diagram (ringkasan)

```
AppUsers (mirror dari Keycloak)

Courses
  ├── CourseModules
  │     └── ModuleAttachments
  ├── Enrollments
  ├── Assignments
  │     └── Submissions
  ├── Quizzes
  │     ├── Questions
  │     │     └── QuestionOptions
  │     └── QuizAttempts
  │           └── AttemptAnswers
  ├── CourseProgresses
  │     └── ModuleProgresses
  ├── ForumPosts (thread + replies, self-referencing)
  ├── CourseReviews
  ├── Announcements
  └── Certificates

CalendarEvents
Notifications
```

### Detail Model

#### AppUser
```csharp
public class AppUser {
    public int    Id          { get; set; }
    public string UserId      { get; set; }  // Keycloak 'sub' claim
    public string Name        { get; set; }
    public string Email       { get; set; }
    public string Role        { get; set; } = "student";  // student|instructor|admin
    public bool   IsActive    { get; set; } = true;
    public DateTime CreatedAt    { get; set; }
    public DateTime LastLoginAt  { get; set; }
}
```

#### Course
```csharp
public class Course {
    public int    Id             { get; set; }
    public string Title          { get; set; }
    public string Description    { get; set; }
    public string? ThumbnailUrl  { get; set; }
    public string InstructorId   { get; set; }  // Keycloak sub
    public string InstructorName { get; set; }
    public string? Category      { get; set; }  // Pemrograman, Desain, dll.
    public string Level          { get; set; } = "Semua"; // Pemula|Menengah|Lanjutan|Semua
    public bool   IsPublished    { get; set; }
    public DateTime CreatedAt    { get; set; }
    public DateTime UpdatedAt    { get; set; }
    // Navigations: Enrollments, Modules, Assignments, Quizzes,
    //              Announcements, ForumPosts, Reviews
}
```

#### CourseModule
```csharp
public class CourseModule {
    public int    Id              { get; set; }
    public int    CourseId        { get; set; }
    public string Title           { get; set; }
    public string? Content        { get; set; }  // HTML dari Quill editor
    public string? VideoUrl       { get; set; }  // URL asli dari user
    public string? VideoEmbedId   { get; set; }  // YouTube/Vimeo ID
    public VideoProvider VideoProvider { get; set; }  // None|YouTube|Vimeo
    public ModuleContentType ContentType { get; set; }  // Text|Video|Mixed
    public int    Order           { get; set; }
    public bool   IsPublished     { get; set; }
    public int    DurationMinutes { get; set; }
    // Navigations: Attachments
}
```

#### Enrollment
```csharp
public class Enrollment {
    public int    Id         { get; set; }
    public int    CourseId   { get; set; }
    public string UserId     { get; set; }
    public string UserName   { get; set; }
    public EnrollmentStatus Status { get; set; }  // Active|Completed|Dropped
    public DateTime EnrolledAt  { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

#### Quiz & Questions
```csharp
public class Quiz {
    public int    Id              { get; set; }
    public int    CourseId        { get; set; }
    public string Title           { get; set; }
    public string? Description    { get; set; }
    public int?   TimeLimitMinutes { get; set; }
    public int    MaxAttempts     { get; set; } = 1;
    public int    PassScore       { get; set; } = 60;
    public DateTime? DueDate      { get; set; }
    public bool   IsPublished     { get; set; }
}

public class Question {
    public int          Id     { get; set; }
    public int          QuizId { get; set; }
    public string       Text   { get; set; }
    public QuestionType Type   { get; set; }  // MultipleChoice|TrueFalse|Essay
    public int          Points { get; set; }
    public int          Order  { get; set; }
    // Navigations: Options
}
```

#### ForumPost (self-referencing)
```csharp
public class ForumPost {
    public int    Id        { get; set; }
    public int    CourseId  { get; set; }
    public int?   ParentId  { get; set; }  // null = thread, ada nilai = reply
    public string UserId    { get; set; }
    public string UserName  { get; set; }
    public string Title     { get; set; }  // hanya untuk thread (ParentId == null)
    public string Body      { get; set; }
    public bool   IsPinned  { get; set; }
    public bool   IsDeleted { get; set; }  // soft delete
    public DateTime CreatedAt { get; set; }
    // Navigations: Parent, Replies
}
```

#### CourseReview
```csharp
public class CourseReview {
    public int    Id        { get; set; }
    public int    CourseId  { get; set; }
    public string UserId    { get; set; }
    public string UserName  { get; set; }
    public int    Rating    { get; set; }   // 1–5
    public string? Comment  { get; set; }
    public DateTime CreatedAt { get; set; }
}
// Unique index: (CourseId, UserId) — satu review per user per kursus
```

#### Notification
```csharp
public class Notification {
    public int    Id        { get; set; }
    public string UserId    { get; set; }
    public string Title     { get; set; }
    public string Message   { get; set; }
    public string? Link     { get; set; }
    public NotificationType Type { get; set; }  // Info|Success|Warning|Assignment|Quiz|Grade|Announcement
    public bool   IsRead    { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### DbContext — LmsDbContext

**DbSets:**
```csharp
// Users
DbSet<AppUser>        AppUsers
// Core
DbSet<Course>         Courses
DbSet<CourseModule>   CourseModules
DbSet<ModuleAttachment> ModuleAttachments
DbSet<Enrollment>     Enrollments
DbSet<Assignment>     Assignments
DbSet<Submission>     Submissions
// Quiz
DbSet<Quiz>           Quizzes
DbSet<Question>       Questions
DbSet<QuestionOption> QuestionOptions
DbSet<QuizAttempt>    QuizAttempts
DbSet<AttemptAnswer>  AttemptAnswers
// Forum & Review
DbSet<ForumPost>      ForumPosts
DbSet<CourseReview>   CourseReviews
// Engagement
DbSet<Announcement>   Announcements
DbSet<CourseProgress> CourseProgresses
DbSet<ModuleProgress> ModuleProgresses
DbSet<Certificate>    Certificates
DbSet<CalendarEvent>  CalendarEvents
DbSet<Notification>   Notifications
```

---

## 5. Controllers & Endpoints

### AccountController
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Account/Login` | Redirect ke Keycloak login |
| GET | `/Account/PostLogin` | Sync user ke DB setelah login |
| GET | `/Account/Logout` | Sign out |
| GET | `/Account/AccessDenied` | Halaman akses ditolak |

### CourseController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Course` | semua | Daftar kursus + filter (search, category, level, sort) |
| GET | `/Course/Details/{id}` | semua | Detail kursus, modul, tugas, forum, review |
| POST | `/Course/Enroll/{id}` | semua | Daftar ke kursus |
| GET | `/Course/Create` | instructor, admin | Form buat kursus |
| POST | `/Course/Create` | instructor, admin | Simpan kursus baru |
| GET | `/Course/Edit/{id}` | instructor, admin | Form edit kursus |
| POST | `/Course/Edit/{id}` | instructor, admin | Update kursus |
| GET | `/Course/MyCourses` | semua | Daftar kursus yang diikuti |

**Query params untuk Index:**
- `search` — kata kunci (judul, deskripsi, instruktur)
- `category` — nama kategori atau `all`
- `level` — `Pemula` / `Menengah` / `Lanjutan` / `Semua` / `all`
- `sort` — `newest` (default) / `oldest` / `popular` / `rating`

### ModuleController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Module/View/{id}` | enrolled/instruktur | Lihat konten modul |
| GET | `/Module/Manage/{courseId}` | instructor, admin | Kelola urutan modul |
| GET | `/Module/Create/{courseId}` | instructor, admin | Form buat modul |
| POST | `/Module/Create` | instructor, admin | Simpan modul + attachment |
| GET | `/Module/Edit/{id}` | instructor, admin | Form edit modul |
| POST | `/Module/Edit/{id}` | instructor, admin | Update modul |
| POST | `/Module/Delete/{id}` | instructor, admin | Hapus modul |
| POST | `/Module/Reorder` | instructor, admin | AJAX reorder (drag-drop) |
| POST | `/Module/DeleteAttachment/{id}` | instructor, admin | Hapus lampiran |

### QuizController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Quiz/Details/{id}` | semua | Info quiz + tombol mulai |
| POST | `/Quiz/Start/{id}` | enrolled | Mulai attempt baru |
| GET | `/Quiz/Take/{attemptId}` | enrolled | Interface pengerjaan quiz |
| POST | `/Quiz/Submit/{attemptId}` | enrolled | Kumpulkan jawaban |
| GET | `/Quiz/Result/{attemptId}` | semua | Hasil quiz |
| POST | `/Quiz/GradeEssay/{answerId}` | instructor, admin | Nilai jawaban essay |
| GET | `/Quiz/Manage/{courseId}` | instructor, admin | Daftar quiz |
| GET | `/Quiz/Create/{courseId}` | instructor, admin | Form buat quiz |
| POST | `/Quiz/Create` | instructor, admin | Simpan quiz |
| GET | `/Quiz/Edit/{id}` | instructor, admin | Form edit quiz |
| POST | `/Quiz/Edit/{id}` | instructor, admin | Update quiz |
| POST | `/Quiz/Delete/{id}` | instructor, admin | Hapus quiz |
| POST | `/Quiz/TogglePublish/{id}` | instructor, admin | Publish/unpublish |

### QuestionController `[Authorize(Roles="instructor,admin")]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Question/Manage/{quizId}` | Daftar soal |
| GET | `/Question/Create/{quizId}` | Form tambah soal |
| POST | `/Question/Create` | Simpan soal |
| POST | `/Question/Delete/{id}` | Hapus soal |
| POST | `/Question/Reorder` | AJAX reorder soal |
| POST | `/Question/ImportCsv/{quizId}` | Import soal dari CSV |

**Format CSV import:**
```
tipe,teks_soal,poin,opsi_a,opsi_b,opsi_c,opsi_d,jawaban_benar
mcq,Apa itu HTTP?,2,Protocol,Language,Database,OS,A
tf,Python adalah bahasa compiled?,1,,,,,False
essay,Jelaskan OOP,5,,,,,
```

### AssignmentController `[Authorize(Roles="instructor,admin")]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Assignment/Manage/{courseId}` | Daftar tugas |
| GET | `/Assignment/Create/{courseId}` | Form buat tugas |
| POST | `/Assignment/Create` | Simpan tugas |
| GET | `/Assignment/Edit/{id}` | Form edit tugas |
| POST | `/Assignment/Edit/{id}` | Update tugas |
| POST | `/Assignment/Delete/{id}` | Hapus tugas |

### SubmissionController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Submission/Submit/{assignmentId}` | enrolled | Form kumpul tugas |
| POST | `/Submission/Submit/{assignmentId}` | enrolled | Upload submission |
| GET | `/Submission/Grade/{assignmentId}` | instructor, admin | Lihat semua submission |
| POST | `/Submission/GradeOne/{submissionId}` | instructor, admin | Beri nilai |
| GET | `/Submission/MyResult/{id}` | semua | Lihat hasil submission |

### ReviewController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| POST | `/Review/Submit` | Kirim/update ulasan (harus enrolled) |
| POST | `/Review/Delete/{id}` | Hapus ulasan (milik sendiri / admin) |

**Body params untuk Submit:**
- `courseId` (int)
- `rating` (int, 1–5)
- `comment` (string, opsional)

### AnalyticsController `[Authorize(Roles="instructor,admin")]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Analytics/Course/{id}` | instructor, admin | Analytics per kursus |
| GET | `/Analytics/Overview` | admin only | Platform overview |

**Data yang ditampilkan di `/Analytics/Course/{id}`:**
- KPI: total peserta, completion rate, avg rating, jumlah modul
- Chart: pendaftaran per bulan (6 bulan terakhir) — Chart.js bar
- Top 5 performers (by % progress)
- Tabel quiz stats (attempt count, avg score, pass rate)
- Tabel assignment stats (submission count, graded, avg score)

**Data yang ditampilkan di `/Analytics/Overview`:**
- KPI: total users, courses, enrollments, completions, quiz attempts, submissions, certificates
- Chart: enrollment trend 12 bulan — Chart.js line
- Top 10 kursus by enrollment
- Top 5 instruktur by total peserta

### ForumController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Forum/Index/{courseId}` | Daftar thread + pagination |
| GET | `/Forum/Thread/{id}` | Thread + replies |
| GET | `/Forum/Create/{courseId}` | Form buat thread |
| POST | `/Forum/Create/{courseId}` | Simpan thread |
| POST | `/Forum/Reply/{threadId}` | Kirim reply |
| POST | `/Forum/Delete/{id}` | Soft-delete post |
| POST | `/Forum/Pin/{id}` | Pin/unpin thread (instructor/admin) |

### AnnouncementController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Announcement/Course/{courseId}` | enrolled/instruktur | Daftar pengumuman |
| GET | `/Announcement/Create/{courseId}` | instructor, admin | Form buat pengumuman |
| POST | `/Announcement/Create` | instructor, admin | Simpan + broadcast notifikasi |
| POST | `/Announcement/Delete/{id}` | instructor, admin | Hapus pengumuman |

### AdminController `[Authorize(Roles="admin")]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Admin` | Dashboard statistik platform |
| GET | `/Admin/Courses` | Daftar semua kursus |
| POST | `/Admin/TogglePublish/{id}` | Publish/unpublish kursus |
| POST | `/Admin/DeleteCourse/{id}` | Hapus kursus |
| GET | `/Admin/Users` | Manajemen user (search, filter role, pagination) |
| POST | `/Admin/SetRole/{userId}` | Ganti role user |
| POST | `/Admin/ToggleUserActive/{userId}` | Aktifkan/nonaktifkan user |

### NotificationController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Notification` | Daftar notifikasi |
| GET | `/Notification/Count` | Jumlah unread (AJAX JSON) |
| POST | `/Notification/MarkRead/{id}` | Tandai satu sudah dibaca |
| POST | `/Notification/MarkAllRead` | Tandai semua sudah dibaca |

### ProgressController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| POST | `/Progress/MarkModuleComplete` | Tandai modul selesai |
| GET | `/Progress/Course/{courseId}` | Get progress data (AJAX) |

### CertificateController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Certificate/View/{id}` | Lihat sertifikat |
| POST | `/Certificate/Generate/{courseId}` | Generate sertifikat (jika kursus selesai) |

### GradebookController `[Authorize]`
| Method | URL | Role | Deskripsi |
|--------|-----|------|-----------|
| GET | `/Gradebook/Course/{courseId}` | instructor, admin | Nilai semua peserta |
| GET | `/Gradebook/My/{courseId}` | enrolled | Nilai saya |

### DashboardController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Dashboard` | Dashboard utama: enrollment, progress, deadlines, notifikasi, sertifikat |

### CalendarController `[Authorize]`
| Method | URL | Deskripsi |
|--------|-----|-----------|
| GET | `/Calendar` | Kalender event dari semua kursus yang diikuti |

---

## 6. Views

### Struktur Folder Views

```
Views/
├── Account/
│   └── AccessDenied.cshtml
├── Admin/
│   ├── Index.cshtml          — Dashboard admin
│   ├── Courses.cshtml        — Semua kursus
│   └── Users.cshtml          — Manajemen user
├── Analytics/
│   ├── Course.cshtml         — Analytics per kursus (Chart.js)
│   └── Overview.cshtml       — Platform overview (Chart.js)
├── Announcement/
│   ├── Course.cshtml         — Daftar pengumuman
│   └── Create.cshtml         — Form buat pengumuman
├── Assignment/
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   ├── Manage.cshtml
│   └── _AssignmentForm.cshtml
├── Calendar/
│   └── Index.cshtml
├── Certificate/
│   └── View.cshtml
├── Course/
│   ├── Index.cshtml          — Daftar kursus + filter sidebar
│   ├── Details.cshtml        — Detail + review section
│   ├── Create.cshtml         — Form buat kursus
│   ├── Edit.cshtml           — Form edit kursus
│   └── MyCourses.cshtml      — Kursus yang diikuti
├── Dashboard/
│   └── Index.cshtml
├── Forum/
│   ├── Index.cshtml          — Daftar thread
│   ├── Thread.cshtml         — Thread + replies
│   └── Create.cshtml
├── Gradebook/
│   ├── Course.cshtml
│   └── My.cshtml
├── Home/
│   └── Index.cshtml
├── Module/
│   ├── View.cshtml           — Konten modul (Quill HTML + video embed)
│   ├── Manage.cshtml         — Drag-drop reorder (Sortable.js)
│   ├── Create.cshtml         — Quill editor + video URL + file upload
│   ├── Edit.cshtml
│   └── _ModuleForm.cshtml
├── Notification/
│   └── Index.cshtml
├── Question/
│   ├── Create.cshtml         — Type picker (MCQ/TF/Essay) + dynamic options
│   └── Manage.cshtml
├── Quiz/
│   ├── Details.cshtml
│   ├── Take.cshtml           — Interface quiz dengan timer
│   ├── Result.cshtml         — Hasil quiz
│   ├── Create.cshtml
│   ├── Edit.cshtml
│   └── Manage.cshtml
├── Shared/
│   ├── _Layout.cshtml        — Layout utama + navbar
│   └── Error.cshtml
├── Submission/
│   ├── Submit.cshtml
│   ├── Grade.cshtml          — Grading semua submission
│   └── MyResult.cshtml       — Hasil submission + grade letter
├── _ViewImports.cshtml
└── _ViewStart.cshtml
```

---

## 7. Services

### IFileUploadService / FileUploadService

```csharp
Task<string> UploadAsync(IFormFile file, string folder)
// Simpan file ke wwwroot/uploads/{folder}/
// Return: path relatif untuk disimpan di DB

void Delete(string filePath)
// Hapus file dari wwwroot

bool IsValidFile(IFormFile file, string[] allowedExtensions, long maxBytes)
// Validasi ekstensi dan ukuran
```

**Lokasi upload:** `wwwroot/uploads/modules/` dan `wwwroot/uploads/submissions/`

### INotificationService / NotificationService

```csharp
Task CreateAsync(string userId, string title, string message, NotificationType type, string? link)
Task CreateForEnrollmentAsync(string userId, string courseName)
Task CreateForGradeAsync(string userId, string assignmentName, int score)
Task CreateForAnnouncementAsync(IEnumerable<string> userIds, string courseName, string title, string link)
Task<int> GetUnreadCountAsync(string userId)
Task MarkAllReadAsync(string userId)
```

Notifikasi otomatis dikirim saat:
- User enroll ke kursus
- Submission selesai dinilai (grade notification)
- Instruktur buat pengumuman baru (broadcast ke semua peserta)

### VideoService (static)

```csharp
(string? embedId, VideoProvider provider) Parse(string? url)
// Mendukung: youtube.com/watch?v=, youtu.be/, vimeo.com/

string GetEmbedUrl(string embedId, VideoProvider provider)
// Return: URL embed untuk iframe

string GetThumbnailUrl(string embedId, VideoProvider provider)
// Return: URL thumbnail (YouTube hqdefault.jpg)
```

### IUserSyncService / UserSyncService

```csharp
Task SyncAsync(string userId, string name, string email)
// Dipanggil di /Account/PostLogin
// Jika user baru: buat AppUser dengan role "student"
// Jika user lama: update Name, Email, LastLoginAt
```

### AppUserClaimsTransformation (IClaimsTransformation)

```csharp
Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
// Dipanggil otomatis di setiap request yang authenticated
// Baca role dari AppUsers.Role
// Inject sebagai ClaimTypes.Role ke ClaimsPrincipal
// Jika IsActive == false: kembalikan principal tanpa role (efektif blokir akses)
```

---

## 8. CSS & JS (Frontend)

### `wwwroot/css/site.css`

**CSS Variables:**
```css
--primary: #006aff
--success: #22c55e
--warning: #f59e0b
--danger: #ef4444
--text: #1a1a2e
--text-muted: #6b7280
--bg: #f8fafc
--border: #e2e8f0
--shadow: 0 2px 8px rgba(0,0,0,0.08)
--radius: 8px
```

**Komponen CSS:**

| Kelas | Fungsi |
|-------|--------|
| `.btn`, `.btn-primary`, `.btn-outline`, `.btn-danger` | Tombol dengan ripple effect |
| `.btn.loading` | State loading (spinner + disabled) |
| `.course-card`, `.course-thumbnail` | Card kursus di Index |
| `.filter-sidebar`, `.chip`, `.chip-active` | Sidebar filter kursus |
| `.star`, `.star.filled` | Tampilan bintang rating (read-only) |
| `.star-pick`, `.star-pick.picked` | Bintang interaktif di form review |
| `.review-card`, `.review-header` | Card ulasan |
| `.kpi-card`, `.kpi-grid` | Analytics KPI cards |
| `.analytics-card`, `.analytics-table` | Layout analytics |
| `.performer-row`, `.progress-bar-wrap` | Top performer list |
| `.form-row` | Grid 2 kolom untuk form fields |
| `.forum-post`, `.forum-reply` | Thread & reply forum |
| `.announcement-card` | Card pengumuman |
| `.quiz-type-btn` | Tombol pilih tipe soal di form |
| `.option-input-row` | Input row untuk opsi MCQ |
| `main` (animation) | Page fade-in saat load |
| `.alert` | Slide-down animation + auto-dismiss |

### `wwwroot/js/site.js`

**Fitur:**

1. **Auto-dismiss alert** — alert hilang otomatis setelah 5 detik dengan animasi fade
2. **Ripple effect** — semua `.btn` punya ripple animasi saat diklik
3. **Form loading state** — tombol submit berubah jadi spinner + "Memproses..." saat form dikirim; reset otomatis setelah 10 detik
4. **Link-btn loading** — `a.btn` navigation juga menampilkan loading state
5. **Form-group focus** — label berubah warna saat input fokus
6. **Navbar active** — link navbar yang sesuai URL aktif di-highlight
7. **Star picker** — interaksi bintang di form review (hover preview, click set nilai)

---

## 9. Fitur Lengkap

### Untuk Student

| Fitur | URL |
|-------|-----|
| Daftar & cari kursus | `/Course` |
| Filter kursus (kategori, level, sort) | `/Course?category=X&level=Y&sort=Z` |
| Detail kursus | `/Course/Details/{id}` |
| Enroll kursus | POST `/Course/Enroll/{id}` |
| Kursus saya | `/Course/MyCourses` |
| Lihat modul | `/Module/View/{id}` |
| Tandai modul selesai | POST `/Progress/MarkModuleComplete` |
| Kerjakan quiz | `/Quiz/Take/{attemptId}` |
| Lihat hasil quiz | `/Quiz/Result/{attemptId}` |
| Kumpul tugas | `/Submission/Submit/{assignmentId}` |
| Lihat nilai tugas | `/Submission/MyResult/{id}` |
| Nilai kursus saya | `/Gradebook/My/{courseId}` |
| Forum diskusi | `/Forum/Index/{courseId}` |
| Lihat pengumuman | `/Announcement/Course/{courseId}` |
| Tulis review kursus | POST `/Review/Submit` |
| Notifikasi | `/Notification` |
| Sertifikat | `/Certificate/View/{id}` |
| Kalender | `/Calendar` |
| Dashboard | `/Dashboard` |

### Untuk Instructor

Semua fitur student + :

| Fitur | URL |
|-------|-----|
| Buat kursus | `/Course/Create` |
| Edit kursus | `/Course/Edit/{id}` |
| Kelola modul (drag-drop) | `/Module/Manage/{courseId}` |
| Buat modul (Quill + video + file) | `/Module/Create/{courseId}` |
| Kelola tugas | `/Assignment/Manage/{courseId}` |
| Nilai submission | `/Submission/Grade/{assignmentId}` |
| Kelola quiz | `/Quiz/Manage/{courseId}` |
| Buat soal (MCQ/TF/Essay) | `/Question/Create/{quizId}` |
| Import soal CSV | POST `/Question/ImportCsv/{quizId}` |
| Buat pengumuman + broadcast | `/Announcement/Create/{courseId}` |
| Pin thread forum | POST `/Forum/Pin/{id}` |
| Analytics kursus | `/Analytics/Course/{id}` |
| Gradebook kursus | `/Gradebook/Course/{courseId}` |

### Untuk Admin

Semua fitur instructor + :

| Fitur | URL |
|-------|-----|
| Dashboard admin | `/Admin` |
| Semua kursus + publish/hapus | `/Admin/Courses` |
| Manajemen user | `/Admin/Users` |
| Ganti role user | POST `/Admin/SetRole/{userId}` |
| Aktifkan/nonaktifkan user | POST `/Admin/ToggleUserActive/{userId}` |
| Platform analytics | `/Analytics/Overview` |

---

## 10. Changelog per Sesi

### Sesi 1 — Fondasi Proyek
- Inisialisasi proyek ASP.NET Core 8 MVC
- Konfigurasi PostgreSQL + EF Core
- Setup Keycloak OIDC authentication
- Model dasar: Course, Enrollment, CourseModule, Assignment, Submission
- Controller: Home, Course (Index, Details, Enroll, Create, MyCourses)
- Layout dasar + site.css awal

### Sesi 2 — Modul & Konten
- **Quill.js rich-text editor** untuk konten modul
- **Video embed** (YouTube & Vimeo) — `VideoService` parse URL → embed ID
- **File attachment** per modul — `FileUploadService`, tabel `ModuleAttachments`
- **Sortable.js drag-drop** reorder modul via AJAX `/Module/Reorder`
- `ModuleController`: Manage, Create, Edit, Delete, Reorder, DeleteAttachment
- Views: Module/Manage, Create, Edit, View, _ModuleForm

### Sesi 3 — Quiz Builder
- Model: Quiz, Question, QuestionOption, QuizAttempt, AttemptAnswer
- `QuizController`: Details, Start, Take, Submit, Result, GradeEssay
- `QuestionController`: Manage, Create, Delete, Reorder
- Quiz timer (countdown JavaScript di Take view)
- Tipe soal: **MCQ**, **True/False**, **Essay**
- Grading otomatis untuk MCQ & TF; manual untuk Essay

### Sesi 4 — Forum, Assignment, User Management, Announcements
- **Forum diskusi** — thread + reply, pin (instruktur), soft-delete, pagination
  - Model: `ForumPost` (self-referencing ParentId)
  - Views: Forum/Index, Thread, Create
- **Assignment CRUD lengkap** — instruktur bisa buat/edit/hapus tugas
  - Views: Assignment/Manage, Create, Edit, _AssignmentForm
- **Submission & Grading** — upload file/teks, grading dengan feedback, grade letter
  - Views: Submission/Submit, Grade, MyResult
- **User Management (Admin)** — lihat semua user, ganti role, aktifkan/nonaktifkan
  - `AdminController`: Users, SetRole, ToggleUserActive
  - Views: Admin/Users
- **Announcement broadcast** — buat pengumuman + notifikasi otomatis ke semua peserta
  - `AnnouncementController` + `INotificationService`
  - Views: Announcement/Course, Create
- **Quiz Builder Enhancement**
  - `QuizController`: Manage, Edit, Delete, TogglePublish
  - `QuestionController`: ImportCsv (format CSV)
  - Views: Quiz/Manage, Edit; Question/Create dengan type picker + dynamic MCQ options
- **User sync** saat login: `IUserSyncService`, `AccountController.PostLogin`
- **Role injection**: `AppUserClaimsTransformation`, model `AppUser`

### Sesi 5 — Button Reactions & UI Feedback
- Ripple effect pada semua `.btn`
- `:active` press-down (scale + brightness)
- Form submit loading state (spinner + "Memproses...")
- Link-btn loading state
- Form-group focus feedback (label color)
- Navbar active link highlight + underline slide animation
- Page fade-in animation (`main`)
- Alert slide-down + auto-dismiss

### Sesi 6 — Search & Filter, Reviews, Analytics
- **Search & Filter kursus** (Course/Index)
  - Filter sidebar: category chips (auto-submit), level chips, sort dropdown
  - Sort: newest, oldest, popular, rating
  - Enhanced course cards: thumbnail, category badge, level badge, star rating display
- **Course Rating & Review**
  - Model `CourseReview`, `ReviewController` (Submit upsert, Delete)
  - Star picker interaktif di form review (JS)
  - Daftar ulasan di Course/Details
  - Course/Create & Edit: tambah field Category + Level
- **Laporan & Analitik**
  - `AnalyticsController`: Course(id), Overview()
  - Analytics/Course.cshtml: KPI cards, Chart.js bar chart (6 bulan), top performers, quiz/assignment stats
  - Analytics/Overview.cshtml: 7 KPI cards, Chart.js line chart (12 bulan), top 10 kursus, top 5 instruktur
  - Chart.js 4.4.0 via CDN
- **EnsureCreated** menggantikan `db.Database.Migrate()` (tidak perlu dotnet-ef SDK)
- NuGet.Config lokal untuk package source mapping

---

## Catatan Teknis Penting

### UserId
Seluruh sistem menggunakan Keycloak `sub` claim sebagai UserId. Cara mengambil:
```csharp
var userId = User.FindFirst("sub")?.Value ?? string.Empty;
```

### Anonymous Type di ViewBag
Saat ViewBag berisi `IEnumerable<dynamic>` dari anonymous types (di AnalyticsController), cara iterasi di Razor:
```razor
@{ var items = ViewBag.SomeData as IEnumerable<dynamic> ?? Enumerable.Empty<dynamic>(); }
@foreach (var item in items) { @item.PropertyName }
```

### Cascade Delete
Semua collection milik Course di-cascade delete. Satu-satunya exception: `ForumPost.Parent → ForumPost.Replies` menggunakan `DeleteBehavior.Restrict` untuk mencegah circular cascade.

### Unique Indexes
| Tabel | Kolom | Alasan |
|-------|-------|--------|
| AppUsers | UserId | Satu AppUser per Keycloak user |
| Enrollments | (CourseId, UserId) | Satu enrollment per user per kursus |
| CourseReviews | (CourseId, UserId) | Satu review per user per kursus |
| CourseProgresses | (CourseId, UserId) | Satu progress per user per kursus |
| ModuleProgresses | (ModuleId, UserId) | Satu progress per user per modul |
| Certificates | (CourseId, UserId) | Satu sertifikat per user per kursus |

# 04 — Skema Database

## Deteksi Database Otomatis

Backend mendeteksi jenis database dari connection string secara otomatis:
- `Data Source=...` → **SQLite**
- `Server=...` / `Initial Catalog=...` / `Trusted_Connection=...` → **SQL Server**
- Selain itu → **PostgreSQL**

---

## Tabel Inti

### AppUser

Tabel pengguna LMS. Role dikelola di sini (bukan di Keycloak).

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | Auto increment |
| UserId | string (unique, indexed) | Keycloak `sub` claim |
| Name | string | Nama tampilan |
| Email | string (unique) | Email dari Keycloak |
| Role | string | `student` / `teacher` / `admin` |
| AvatarUrl | string? | URL foto profil |
| IsActive | bool | Status akun |
| CreatedAt | datetime | Waktu pendaftaran pertama |
| LastLoginAt | datetime? | Login terakhir |

---

### Course

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| Title | string (max 200) | Judul kursus |
| Description | text | Deskripsi lengkap |
| InstructorId | int (FK AppUser) | Instruktur pembuat |
| InstructorName | string | Cache nama instruktur |
| Category | string | Kategori kursus |
| Level | string | `Pemula` / `Menengah` / `Mahir` / `Semua` |
| ThumbnailUrl | string? | URL gambar thumbnail |
| IsPublished | bool | Apakah kursus dipublikasi |
| CreatedAt | datetime | |
| UpdatedAt | datetime | |

Relasi:
- `Enrollments` (1:∞, cascade delete)
- `Sections` (1:∞, cascade delete)
- `Modules` (1:∞, cascade delete)
- `Assignments` (1:∞, cascade delete)
- `Quizzes` (1:∞, cascade delete)
- `Announcements` (1:∞, cascade delete)
- `ForumPosts` (1:∞, cascade delete)
- `Certificates` (1:∞, cascade delete)
- `CompletionRule` (1:1, cascade delete)
- `GradeItems` (1:∞, cascade delete)

---

### CourseSection

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| Title | string | Judul seksi |
| Description | string? | |
| Order | int | Urutan tampil |
| IsVisible | bool | Visible ke mahasiswa |

Index: `(CourseId, Order)` untuk ordering efisien.

Saat section dihapus: modul di dalamnya **tidak terhapus** (SectionId di-set null).

---

### CourseModule

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| SectionId | int? (FK CourseSection, SetNull) | |
| Title | string | |
| Content | text? | Konten teks/HTML |
| VideoUrl | string? | URL video asli |
| VideoEmbedId | string? | ID embed (YouTube/Vimeo) |
| VideoProvider | enum | `None` / `YouTube` / `Vimeo` |
| ContentType | enum | `Text` / `Video` / `Mixed` |
| Order | int | |
| IsPublished | bool | |
| DurationMinutes | int | Estimasi durasi |

Relasi:
- `Attachments` → `ModuleAttachment` (1:∞, cascade delete)

---

### Enrollment

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| UserId | string | Keycloak sub |
| Status | enum | `Active` / `Completed` / `Dropped` |
| EnrolledAt | datetime | |
| CompletedAt | datetime? | |

Index unik: `(CourseId, UserId)`

---

## Sistem Quiz

### Quiz

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| Title | string | |
| Description | text? | |
| TimeLimitMinutes | int? | Null = tidak ada limit |
| MaxAttempts | int | 0 = unlimited |
| PassScore | decimal | Nilai minimum lulus (0–100) |
| DueDate | datetime? | |
| IsPublished | bool | |

### Question (untuk Quiz)

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| QuizId | int (FK Quiz, cascade) | |
| Text | text | Teks soal |
| Type | enum | `MultipleChoice` / `TrueFalse` / `Essay` |
| Order | int | |

Relasi: `Options` → `QuestionOption` (1:∞, cascade)

### QuizAttempt

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| QuizId | int (FK Quiz, cascade) | |
| UserId | string | |
| StartedAt | datetime | |
| SubmittedAt | datetime? | |
| Score | decimal? | |

Relasi: `Answers` → `AttemptAnswer` (1:∞, cascade)

### AttemptAnswer

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| AttemptId | int (FK QuizAttempt, cascade) | |
| QuestionId | int (FK Question) | |
| SelectedOptionId | int? | Untuk MC/TF |
| EssayAnswer | text? | Untuk esai |
| IsCorrect | bool? | |
| EarnedPoints | decimal? | |

---

## Bank Soal

### QuestionBank

Bank soal global (bisa dipakai di berbagai quiz/ujian).

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| OwnerId | string | UserId pembuat |
| Text | text | Teks soal |
| Type | enum | `MultipleChoice` / `TrueFalse` / `Essay` |
| Points | decimal | Poin soal |
| Explanation | text? | Penjelasan jawaban |

Relasi: `Options` → `QuestionBankOption` (1:∞, cascade)

> **Catatan penting**: `PracticeAttemptAnswer` referensi ke `QuestionBank` dengan constraint **Restrict** — artinya soal bank **tidak bisa dihapus** jika ada jawaban attempt yang masih mereferensikan soal tersebut.

### CourseQuestionBank

Bank soal per-kursus (private, dikelola oleh instruktur kursus).

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| ModuleId | int? (FK CourseModule, SetNull) | Soal terkait modul tertentu |
| Text | text | |
| Type | enum | |
| Points | decimal | |
| Explanation | text? | |
| CreatedBy | string | UserId |
| CreatedAt | datetime | |

---

## 4 Jenis Ujian

### 1. Practice Quiz (Latihan Mandiri)

```
PracticeQuiz
  └── PracticeAttempt
        └── PracticeAttemptAnswer
              └── (FK) QuestionBank
```

| Model | Keterangan |
|-------|-----------|
| PracticeQuiz | Konfigurasi: judul, jumlah soal, shuffle, time limit |
| PracticeAttempt | Satu sesi pengerjaan |
| PracticeAttemptAnswer | Jawaban per soal, referensi ke QuestionBank |

### 2. Question Set / Ujian Formal

```
QuestionSet
  ├── QuestionSetQuestion
  │     └── QuestionSetOption
  └── QuestionSetAttempt
        └── QuestionSetAnswer
```

| Model | Keterangan |
|-------|-----------|
| QuestionSet | Ujian formal buatan instruktur, bisa diimpor dari bank soal |
| QuestionSetQuestion | Soal dalam ujian ini (standalone, bukan referensi bank) |
| QuestionSetAttempt | Satu sesi pengerjaan |
| QuestionSetAnswer | Jawaban, penilaian otomatis MC dan manual esai |

### 3. Standalone Exam

```
Exam
  ├── ExamQuestion
  │     └── ExamQuestionOption
  └── ExamAttempt
        └── ExamAnswer
```

Mirip Question Set tapi lebih mandiri, tidak terikat kursus.

### 4. Mandatory Exam (Deep-Link / Ujian Wajib)

```
MandatoryExam
  ├── MandatoryExamQuestion
  │     └── MandatoryExamOption
  ├── MandatoryExamAssignment
  │     └── MandatoryExamAttempt
  │           └── MandatoryExamAnswer
  └── MandatoryExamSession     (audit trail token)
```

**MandatoryExam**

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| Title | string | |
| Description | text? | |
| TimeLimitMinutes | int? | |
| MaxAttempts | int | |
| PassScore | decimal | |
| PublicAccessCode | string? (unique) | UUID untuk akses publik |
| WebhookUrl | string? | Callback DWI Mobile saat selesai |
| IsActive | bool | |

**MandatoryExamAssignment**

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| ExamId | int (FK, cascade) | |
| UserId | string | |
| Status | enum | `NotYet` / `InProgress` / `Done` |
| AssignedAt | datetime | |
| CompletedAt | datetime? | |

Index unik: `(ExamId, UserId)`

**MandatoryExamSession** (Audit Trail Token)

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| ExamId | int (FK, cascade) | |
| UserId | string | |
| TokenJti | string (unique) | JWT `jti` claim — ID unik token |
| GeneratedBy | string | Admin yang generate |
| GeneratedAt | datetime | |
| ExpiresAt | datetime | |
| UsedAt | datetime? | Kapan pertama dipakai |
| IsRevoked | bool | |
| IsLinkToken | bool | true = link publik, false = personal |
| ParentSessionId | int? | Derived dari link token |
| MaxUsageCount | int | 0 = unlimited (untuk link token) |
| CurrentUsageCount | int | Berapa kali sudah dipakai |

---

## Forum

### ForumPost

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK Course, cascade) | |
| ParentId | int? (FK self, Restrict) | null = thread, isi = balasan |
| RootThreadId | int? | **Denormalisasi**: semua reply menunjuk ke root thread |
| UserId | string | |
| UserName | string | Cache nama user |
| Title | string? | Hanya untuk thread (ParentId null) |
| Body | text | |
| IsPinned | bool | |
| IsDeleted | bool | Soft delete |
| CreatedAt | datetime | |
| UpdatedAt | datetime | |
| EditedAt | datetime? | |

Relasi: `Likes` → `ForumLike` (1:∞, cascade)

> **Kenapa RootThreadId?** Denormalisasi ini memungkinkan loading seluruh thread (termasuk balasan bersarang unlimited) dalam satu query, tanpa perlu recursive query yang mahal.

### ForumLike

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| PostId | int (FK ForumPost, cascade) | |
| UserId | string | |

Index unik: `(PostId, UserId)` — satu user hanya bisa like sekali per post.

---

## Gradebook & Progress

### CourseGradeItem

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (FK, cascade) | |
| Name | string | Nama komponen nilai |
| Order | int | Urutan tampil |

Relasi: `Entries` → `CourseGradeEntry` (1:∞, cascade)

### CourseGradeEntry

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| GradeItemId | int (FK, cascade) | |
| UserId | string | |
| Score | decimal | |
| Feedback | text? | |

Index unik: `(GradeItemId, UserId)`

### CourseProgress

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int | |
| UserId | string | |
| CompletedModuleCount | int | |
| TotalModuleCount | int | |

Computed: `Percentage = CompletedModuleCount / TotalModuleCount * 100`

Index unik: `(CourseId, UserId)`

### ModuleProgress

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| ModuleId | int (FK, cascade) | |
| UserId | string | |
| IsCompleted | bool | |

Index unik: `(ModuleId, UserId)`

### CourseCompletionRule

Aturan penyelesaian kursus — relasi 1:1 dengan Course.

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| CourseId | int (unique FK, cascade) | |
| Rules | JSON | Aturan custom (modul wajib, quiz wajib, nilai min, dll.) |

---

## Komunikasi & Notifikasi

### Notification

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| UserId | string (indexed) | |
| Title | string | |
| Message | text | |
| Type | enum | `Info` / `Success` / `Grade` / `Assignment` / `Announcement` / `Quiz` / `Certificate` |
| Link | string? | URL relatif untuk navigasi |
| IsRead | bool | |
| CreatedAt | datetime | |

Index: `(UserId, IsRead)` untuk query notifikasi belum dibaca.

### Conversation & Message

**Conversation**

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| User1Id | string | |
| User2Id | string | |
| LastMessageAt | datetime | |

Index unik: pasangan `(User1Id, User2Id)` — satu percakapan per pasang user.

**Message**

| Kolom | Tipe | Keterangan |
|-------|------|------------|
| Id | int (PK) | |
| ConversationId | int (FK, cascade) | |
| SenderId | string | |
| Content | text | |
| IsRead | bool | |
| CreatedAt | datetime | |

---

## Tabel Pendukung Lainnya

| Tabel | Keterangan |
|-------|-----------|
| `Announcement` | Pengumuman per-kursus (judul, konten, pin, timestamp) |
| `Certificate` | Sertifikat per user per kursus (unique: CourseId+UserId) |
| `ActivityLog` | Audit trail aksi user (action, entityType, metadata JSON, timestamp) |
| `CalendarEvent` | Event kalender (kursus, tipe, tanggal mulai/selesai) |
| `CourseReview` | Ulasan kursus dengan rating |
| `Attendance` | Sesi kehadiran + record per mahasiswa |
| `CoursePrerequisite` | Prasyarat kursus (FK antar Course) |
| `CourseResource` | Materi tambahan per kursus (file/link) |
| `ModuleAttachment` | Lampiran file per modul |
| `Assignment` | Tugas per kursus (judul, due date, max score) |
| `Submission` | Submit tugas (file, skor, feedback) |

---

## Diagram Relasi Ringkas

```
AppUser ──────────── Course (InstructorId)
         │
         ├────────── Enrollment (UserId) ──── Course
         ├────────── ForumPost (UserId)
         ├────────── Notification (UserId)
         └────────── ActivityLog (UserId)

Course ─────────────┬─ CourseSection ──── CourseModule (SectionId, SetNull)
                    ├─ CourseModule (direct)
                    ├─ Enrollment
                    ├─ Assignment ──── Submission
                    ├─ Quiz ──── Question ──── QuestionOption
                    │          └─ QuizAttempt ──── AttemptAnswer
                    ├─ Announcement
                    ├─ ForumPost (self-referential, RootThreadId)
                    │  └─ ForumLike
                    ├─ Certificate
                    ├─ CourseCompletionRule (1:1)
                    ├─ CourseGradeItem ──── CourseGradeEntry
                    ├─ CourseProgress ──── ModuleProgress
                    └─ CourseQuestionBank

MandatoryExam ──────┬─ MandatoryExamQuestion
                    ├─ MandatoryExamAssignment ──── MandatoryExamAttempt
                    └─ MandatoryExamSession (audit trail)

QuestionBank ──── QuestionBankOption
             └─── PracticeAttemptAnswer (Restrict — tidak bisa hapus jika ada jawaban)
```

---

## Catatan Cascade Delete (SQL Server)

SQL Server tidak mengizinkan multiple cascade paths ke tabel yang sama. Beberapa relasi menggunakan `NoAction` (tidak cascade) untuk menghindari error ini:

- `MandatoryExamAttempt` → `MandatoryExamQuestion`: NoAction
- Beberapa FK di Assignment/Submission

Untuk PostgreSQL dan SQLite, cascade + SetNull diizinkan secara normal.

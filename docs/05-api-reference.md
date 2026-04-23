# 05 — Referensi API

Base URL: `http://localhost:5000/api`

Swagger UI: `http://localhost:5000/swagger`

## Konvensi

**Auth**:
- `Public` = tidak perlu token
- `JWT` = perlu Bearer token
- `JWT (teacher)` = perlu Bearer token + role teacher/admin
- `JWT (admin)` = perlu Bearer token + role admin

**Format Response Sukses (list)**:
```json
{
  "items": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

**Format Response Error**:
```json
{ "message": "Pesan error" }
```

---

## Auth (`/api/auth`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| POST | `/sync` | JWT | Sync user Keycloak ke database LMS (create/update AppUser) |
| GET | `/me` | JWT | Ambil profil user yang sedang login |
| PUT | `/profile` | JWT | Update nama profil |
| POST | `/avatar` | JWT | Upload foto profil (jpg/png/webp, maks 5MB) |
| GET | `/debug-claims` | JWT | **Dev only** — dump semua JWT claims untuk debugging |

---

## Courses (`/api/courses`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/courses` | Public | List semua kursus. Query params: `search`, `category`, `level`, `sort`, `page`, `pageSize` |
| GET | `/courses/my` | JWT | Teacher: kursus saya. Student: kursus yang diikuti |
| GET | `/courses/{id}` | Public | Detail kursus + seksi + modul + tugas + quiz |
| POST | `/courses` | JWT (teacher) | Buat kursus baru |
| PUT | `/courses/{id}` | JWT (teacher) | Update kursus |
| DELETE | `/courses/{id}` | JWT (teacher) | Hapus kursus |
| POST | `/courses/{id}/enroll` | JWT | Daftar ke kursus |
| POST | `/courses/{id}/reviews` | JWT | Submit ulasan kursus |
| POST | `/courses/{id}/thumbnail` | JWT (teacher) | Upload thumbnail kursus |

---

## Course Sections (`/api/courses/{courseId}/sections`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List semua seksi (terurut) |
| POST | `/` | JWT (teacher) | Buat seksi baru |
| PUT | `/{id}` | JWT (teacher) | Update seksi |
| DELETE | `/{id}` | JWT (teacher) | Hapus seksi (modul tidak ikut terhapus) |

---

## Modules (`/api/courses/{courseId}/modules`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List modul dalam kursus |
| GET | `/{id}` | JWT | Detail modul |
| POST | `/` | JWT (teacher) | Buat modul baru |
| PUT | `/{id}` | JWT (teacher) | Update modul |
| DELETE | `/{id}` | JWT (teacher) | Hapus modul |
| POST | `/reorder` | JWT (teacher) | Ubah urutan modul |
| POST | `/{id}/attachments` | JWT (teacher) | Upload lampiran ke modul |
| DELETE | `/{id}/attachments/{attId}` | JWT (teacher) | Hapus lampiran |

---

## Assignments (`/api/courses/{courseId}/assignments`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List tugas dalam kursus |
| GET | `/{id}` | JWT | Detail tugas |
| POST | `/` | JWT (teacher) | Buat tugas baru |
| PUT | `/{id}` | JWT (teacher) | Update tugas |
| DELETE | `/{id}` | JWT (teacher) | Hapus tugas |
| POST | `/{id}/submit` | JWT | Submit jawaban tugas (file upload) |
| GET | `/{id}/submissions` | JWT (teacher) | List semua submission (tampilan teacher) |
| PUT | `/submissions/{subId}/grade` | JWT (teacher) | Beri nilai + feedback |

---

## Quizzes

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/courses/{courseId}/quizzes` | JWT | List quiz dalam kursus |
| GET | `/quizzes/{id}` | JWT | Detail quiz + soal + pilihan |
| POST | `/courses/{courseId}/quizzes` | JWT (teacher) | Buat quiz baru |
| PUT | `/quizzes/{id}` | JWT (teacher) | Update quiz (kirim notifikasi jika baru dipublish) |
| DELETE | `/quizzes/{id}` | JWT (teacher) | Hapus quiz |
| GET | `/quizzes/{quizId}/questions` | JWT (teacher) | List soal quiz |
| POST | `/quizzes/{quizId}/questions` | JWT (teacher) | Tambah soal |
| PUT | `/questions/{id}` | JWT (teacher) | Update soal |
| DELETE | `/questions/{id}` | JWT (teacher) | Hapus soal |
| POST | `/quizzes/{quizId}/start` | JWT | Mulai attempt quiz |
| POST | `/attempts/{attemptId}/submit` | JWT | Submit jawaban quiz |
| GET | `/attempts/{attemptId}/result` | JWT | Lihat hasil attempt |
| POST | `/attempt-answers/{answerId}/grade` | JWT (teacher) | Nilai jawaban esai manual |
| POST | `/quizzes/{quizId}/import-from-bank` | JWT (teacher) | Import soal dari bank soal |

---

## Practice Quiz (`/api/practice`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/practice` | Public | List semua practice quiz |
| POST | `/practice` | JWT (teacher) | Buat practice quiz |
| POST | `/practice/{id}/attempts` | JWT | Mulai attempt |
| POST | `/practice/attempts/{id}/submit` | JWT | Submit jawaban |
| GET | `/practice/attempts/{id}/result` | JWT | Lihat hasil |

---

## Question Set / Ujian (`/api/ujian`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/ujian` | JWT | List semua question set |
| GET | `/ujian/{id}` | JWT | Detail question set |
| POST | `/ujian` | JWT (teacher) | Buat question set baru |
| PUT | `/ujian/{id}` | JWT (teacher) | Update question set |
| DELETE | `/ujian/{id}` | JWT (teacher) | Hapus question set |
| POST | `/ujian/{id}/questions` | JWT (teacher) | Tambah soal |
| PUT | `/ujian/questions/{id}` | JWT (teacher) | Update soal |
| DELETE | `/ujian/questions/{id}` | JWT (teacher) | Hapus soal |
| POST | `/ujian/{id}/import-from-bank` | JWT (teacher) | Import dari bank soal |
| POST | `/ujian/{id}/attempts` | JWT | Mulai attempt |
| POST | `/ujian/attempts/{id}/submit` | JWT | Submit jawaban |
| GET | `/ujian/attempts/{id}/result` | JWT | Hasil attempt |
| POST | `/ujian/attempt-answers/{id}/grade` | JWT (teacher) | Nilai esai manual |
| GET | `/ujian/{id}/attempts` | JWT (teacher) | Semua attempt (teacher view) |
| GET | `/ujian/{id}/export` | JWT (teacher) | Export hasil ke CSV/Excel |

---

## Mandatory Exam / Ujian Wajib (`/api/mandatory-exams`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/mandatory-exams` | JWT (teacher) | List semua mandatory exam |
| GET | `/mandatory-exams/{id}` | JWT | Detail mandatory exam |
| POST | `/mandatory-exams` | JWT (teacher) | Buat mandatory exam baru |
| PUT | `/mandatory-exams/{id}` | JWT (teacher) | Update |
| DELETE | `/mandatory-exams/{id}` | JWT (teacher) | Hapus |
| POST | `/mandatory-exams/{id}/questions` | JWT (teacher) | Tambah soal |
| PUT | `/mandatory-exams/questions/{id}` | JWT (teacher) | Update soal |
| DELETE | `/mandatory-exams/questions/{id}` | JWT (teacher) | Hapus soal |
| POST | `/mandatory-exams/{id}/assign` | JWT (teacher) | Assign ujian ke user(s) |
| POST | `/mandatory-exams/{id}/generate-link` | JWT (teacher) | Generate deep-link (return URL + token) |
| POST | `/mandatory-exams/{id}/generate-access-code` | JWT (teacher) | Generate kode akses publik |
| GET | `/mandatory-exams/{id}/sessions` | JWT (teacher) | List sesi token (audit trail) |
| POST | `/mandatory-exams/sessions/{sessionId}/revoke` | JWT (teacher) | Cabut token |
| POST | `/mandatory-exams/start` | X-Exam-Token | Mulai ujian via token deep-link |
| POST | `/mandatory-exams/attempts/{id}/submit` | JWT | Submit jawaban |
| GET | `/mandatory-exams/attempts/{id}/result` | JWT | Hasil attempt |
| GET | `/mandatory-exams/{id}/results` | JWT (teacher) | Semua hasil (teacher view) |
| GET | `/mandatory-exams/{id}/export` | JWT (teacher) | Export hasil ke CSV/Excel |
| POST | `/mandatory-exams/{id}/import-from-bank` | JWT (teacher) | Import soal dari bank soal |

---

## Standalone Exam (`/api/exams`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/exams` | JWT | List semua exam |
| GET | `/exams/{id}` | JWT | Detail exam |
| POST | `/exams` | JWT (teacher) | Buat exam |
| PUT | `/exams/{id}` | JWT (teacher) | Update |
| DELETE | `/exams/{id}` | JWT (teacher) | Hapus |
| POST | `/exams/{id}/questions` | JWT (teacher) | Tambah soal |
| PUT | `/exams/questions/{id}` | JWT (teacher) | Update soal |
| DELETE | `/exams/questions/{id}` | JWT (teacher) | Hapus soal |
| POST | `/exams/{id}/attempts` | JWT | Mulai attempt |
| POST | `/exams/attempts/{id}/submit` | JWT | Submit |
| GET | `/exams/attempts/{id}/result` | JWT | Hasil |

---

## Forum (`/api/courses/{courseId}/forum`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List thread (pinned dulu, lalu terbaru) |
| GET | `/{threadId}` | JWT | Detail thread + semua balasan bersarang |
| POST | `/` | JWT | Buat thread baru |
| POST | `/{threadId}/reply` | JWT | Balas thread (unlimited nesting) |
| PUT | `/posts/{postId}` | JWT | Update post (hanya post sendiri) |
| DELETE | `/posts/{postId}` | JWT | Hapus post (teacher/admin bisa hapus milik siapapun) |
| POST | `/{threadId}/pin` | JWT (teacher) | Toggle pin thread |
| POST | `/posts/{postId}/like` | JWT | Toggle like/unlike |

---

## Gradebook (`/api/courses/{courseId}/gradebook`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | Matriks nilai (mahasiswa × komponen nilai) |
| POST | `/items` | JWT (teacher) | Buat komponen nilai baru |
| PUT | `/items/{id}` | JWT (teacher) | Update komponen nilai |
| DELETE | `/items/{id}` | JWT (teacher) | Hapus komponen nilai |
| PUT | `/entries/{id}` | JWT (teacher) | Update satu entri nilai |

---

## Notifications (`/api/notifications`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | Ambil semua notifikasi user |
| GET | `/unread-count` | JWT | Jumlah notifikasi belum dibaca |
| PUT | `/{id}/read` | JWT | Tandai satu notifikasi sudah dibaca |
| POST | `/mark-all-read` | JWT | Tandai semua sudah dibaca |

---

## Dashboard (`/api/dashboard`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | Data dashboard sesuai role (student/teacher/admin) |

**Student response**: enrolled courses, kemajuan, upcoming assignments, pengumuman terbaru.

**Teacher response**: stats mahasiswa, submission pending, kesehatan kursus.

**Admin response**: metrik sistem keseluruhan.

---

## Course Resources (`/api/courses/{courseId}/resources`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List materi tambahan |
| POST | `/` | JWT (teacher) | Upload materi baru |
| PUT | `/{id}` | JWT (teacher) | Update info materi |
| DELETE | `/{id}` | JWT (teacher) | Hapus materi |

---

## Course Question Bank (`/api/courses/{courseId}/question-bank`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT (teacher) | List soal bank kursus ini |
| POST | `/` | JWT (teacher) | Tambah soal ke bank kursus |
| PUT | `/{id}` | JWT (teacher) | Update soal |
| DELETE | `/{id}` | JWT (teacher) | Hapus soal |
| POST | `/bulk` | JWT (teacher) | Tambah banyak soal sekaligus |

---

## AI Question Generation (`/api/ai`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| POST | `/ai/generate-questions` | JWT (teacher) | Generate soal otomatis dengan LLM |

**Request body**:
```json
{
  "courseId": 1,
  "moduleId": 2,
  "questionCount": 10,
  "questionType": "MultipleChoice",
  "difficulty": "medium",
  "topic": "Topik khusus (opsional)"
}
```

**Response**: Array soal dalam format bank soal, siap disimpan.

---

## Attendance (`/api/courses/{courseId}/attendance`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/sessions` | JWT | List sesi kehadiran |
| POST | `/sessions` | JWT (teacher) | Buat sesi kehadiran baru |
| GET | `/sessions/{id}` | JWT | Detail sesi + daftar hadir |
| POST | `/sessions/{id}/mark` | JWT (teacher) | Tandai hadir/absen |

---

## Calendar (`/api/calendar`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List event kalender (filter by range tanggal) |
| POST | `/` | JWT (teacher) | Buat event baru |
| PUT | `/{id}` | JWT (teacher) | Update event |
| DELETE | `/{id}` | JWT (teacher) | Hapus event |

---

## Certificates (`/api/certificates`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List sertifikat milik user yang login |
| GET | `/{id}` | JWT | Detail sertifikat |
| GET | `/verify/{number}` | Public | Verifikasi keaslian sertifikat via nomor sertifikat |

---

## Messages / Direct Message (`/api/messages`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/conversations` | JWT | List percakapan saya |
| POST | `/conversations` | JWT | Mulai percakapan baru |
| GET | `/conversations/{id}/messages` | JWT | List pesan dalam percakapan |
| POST | `/conversations/{id}/messages` | JWT | Kirim pesan |
| PUT | `/conversations/{id}/read` | JWT | Tandai percakapan sudah dibaca |

---

## Activity Log (`/api/activity`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | Log aktivitas user yang login |
| GET | `/course/{courseId}` | JWT (teacher) | Log aktivitas per kursus |

---

## Search (`/api/search`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | Global search. Query params: `q`, `type` (courses/assignments/forum) |

---

## Admin (`/api/admin`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/dashboard` | JWT (admin) | Statistik sistem keseluruhan |
| GET | `/users` | JWT (admin) | List semua user |
| PUT | `/users/{id}/role` | JWT (admin) | Ubah role user |
| GET | `/courses` | JWT (admin) | List semua kursus (admin view) |
| PUT | `/courses/{id}/publish` | JWT (admin) | Toggle publikasi kursus |

---

## Reports (`/api/reports`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/courses/{courseId}/grades` | JWT (teacher) | Export nilai kursus (CSV/Excel) |
| GET | `/courses/{courseId}/attendance` | JWT (teacher) | Export kehadiran |
| GET | `/courses/{courseId}/progress` | JWT (teacher) | Export kemajuan mahasiswa |

---

## Prerequisites (`/api/courses/{courseId}/prerequisites`)

| Method | Endpoint | Auth | Deskripsi |
|--------|----------|------|-----------|
| GET | `/` | JWT | List prasyarat kursus |
| POST | `/` | JWT (teacher) | Tambah prasyarat |
| DELETE | `/{id}` | JWT (teacher) | Hapus prasyarat |

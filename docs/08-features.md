# 08 — Panduan Fitur

## 1. Manajemen Kursus

### Membuat Kursus (Teacher)

1. Login sebagai teacher
2. Buka **Kursus** → **Buat Kursus**
3. Isi:
   - Judul (maks 200 karakter)
   - Deskripsi
   - Kategori
   - Level (Pemula / Menengah / Mahir / Semua)
4. Upload thumbnail (opsional)
5. Simpan — status awal: **Draft** (belum dipublikasi)
6. Untuk publish: **Edit Kursus** → toggle **Publikasi**

### Struktur Kursus

```
Kursus
├── Seksi 1 (OrderAble)
│   ├── Modul 1.1
│   ├── Modul 1.2
│   └── Modul 1.3
└── Seksi 2
    └── Modul 2.1

Modul tanpa seksi (bisa ada)
```

**Seksi** bersifat opsional — modul bisa tidak punya seksi.
Jika seksi dihapus, modul di dalamnya **tidak ikut terhapus** (SectionId menjadi null).

### Modul

Setiap modul bisa berisi:
- **Teks** — konten HTML/richtext
- **Video** — YouTube atau Vimeo (via embed ID)
- **Mixed** — kombinasi teks dan video
- **Lampiran file** — PDF, dokumen, gambar

### Prasyarat Kursus

Teacher bisa menetapkan kursus lain sebagai prasyarat:
- **Blocking**: mahasiswa tidak bisa daftar sebelum prasyarat selesai
- **Warning**: peringatan ditampilkan tapi tetap bisa daftar

---

## 2. Enrollment (Pendaftaran Kursus)

**Student**:
1. Buka halaman detail kursus
2. Klik **Daftar**
3. Jika ada prasyarat dan belum selesai → tampil peringatan / blocked

**Status enrollment**:
- `Active` — sedang terdaftar
- `Completed` — kursus selesai
- `Dropped` — keluar dari kursus

---

## 3. Sistem Quiz

### Alur Quiz

```
Teacher buat quiz → Tambah soal → Publish
Student buka quiz → Klik "Mulai" → Timer mulai
Student jawab soal → Submit
Backend auto-grade MC/TF → Esai perlu dinilai manual
Student lihat hasil
```

### Tipe Soal

| Tipe | Keterangan |
|------|-----------|
| `MultipleChoice` | Pilihan ganda, satu jawaban benar |
| `TrueFalse` | Benar atau salah |
| `Essay` | Jawaban teks bebas, dinilai manual |

### Pengaturan Quiz

| Setting | Keterangan |
|---------|-----------|
| Batas waktu | Menit (kosong = tidak ada batas) |
| Maks percobaan | 0 = unlimited |
| Pass score | Nilai minimum lulus (0–100) |
| Due date | Batas terakhir mengerjakan (opsional) |

### Import Soal dari Bank Soal

Teacher bisa mengimpor soal dari bank soal global:
1. Buka manage quiz
2. **Import dari Bank Soal**
3. Pilih soal yang diinginkan
4. Konfirmasi — soal dikopi (bukan referensi) ke dalam quiz

---

## 4. Empat Jenis Ujian

### 4.1 Quiz (per-Kursus)

- Terikat ke satu kursus
- Hanya mahasiswa yang enrolled bisa mengerjakan
- Bisa diakses dari halaman detail kursus

### 4.2 Practice Quiz (Latihan Mandiri)

- Tidak perlu enrollment kursus
- Bisa diakses siapa saja yang login
- Hasil langsung (auto-grade semua tipe soal)
- Untuk latihan self-assessment

### 4.3 Question Set / Ujian Formal

- Dibuat oleh teacher
- Tidak harus terikat kursus
- Soal bisa dikimpor dari bank soal
- Mendukung grading manual esai oleh teacher
- Export hasil ke CSV/Excel

### 4.4 Mandatory Exam / Ujian Wajib

**Fitur khusus** untuk integrasi DWI Mobile:

**Distribusi via Deep-Link**:
1. Admin generate link dari dashboard
2. Link berbentuk: `http://localhost:5173/exam/start?token={jwt}`
3. Kirim link ke peserta (email, WhatsApp, dll.)
4. Peserta buka link → langsung ke halaman ujian
5. Tidak perlu login Keycloak — menggunakan token ujian

**Distribusi via Kode Akses**:
1. Admin generate kode akses (UUID publik)
2. Mahasiswa masukkan kode di halaman ujian wajib
3. Sistem validasi kode → mulai ujian

**Audit Trail Token**:
- Setiap token dicatat di `MandatoryExamSession`
- Admin bisa melihat: siapa yang generate, kapan dipakai, berapa kali
- Token bisa dicabut (revoke) sebelum dipakai

**Webhook**:
- Saat peserta selesai ujian, sistem POST ke `WebhookUrl` (jika dikonfigurasi)
- DWI Mobile bisa otomatis memproses hasil

---

## 5. Tugas (Assignment)

### Alur Tugas

```
Teacher buat tugas (judul, deskripsi, due date, maks skor)
Student upload file jawaban (sebelum due date)
Teacher beri nilai + feedback
Student lihat nilai dan feedback
```

### Format File yang Diterima

Tergantung konfigurasi `IFileUploadService` — umumnya:
- Dokumen: PDF, DOCX, XLSX
- Gambar: JPG, PNG
- Maks ukuran: 50MB

---

## 6. Gradebook (Buku Nilai)

Tampilan matriks: **baris = mahasiswa**, **kolom = komponen nilai**

Teacher bisa:
- Buat komponen nilai (assignment, quiz, partisipasi, dsb.)
- Input nilai per-mahasiswa per-komponen
- Lihat total/rata-rata
- Export ke CSV/Excel

---

## 7. Bank Soal

### Bank Soal Global (`QuestionBank`)

- Dikelola oleh teacher/admin
- Soal bisa dipakai di berbagai quiz dan ujian
- **Perhatian**: Soal tidak bisa dihapus jika ada attempt yang sudah menjawab soal tersebut

### Bank Soal Per-Kursus (`CourseQuestionBank`)

- Private per-kursus
- Dikelola oleh instruktur kursus
- Bisa diimpor ke quiz atau question set dalam kursus tersebut
- Bisa dihubungkan ke modul tertentu

### Generasi Soal dengan AI

1. Buka **Bank Soal** kursus
2. Klik **Generate dengan AI**
3. Pilih:
   - Jumlah soal
   - Tipe soal
   - Topik (opsional — AI akan pakai konteks modul kursus)
4. AI generate soal → tampil untuk direview
5. Pilih soal mana yang disimpan ke bank soal

**Provider AI**: DekaLLM (`https://dekallm.cloudeka.ai/v1`)  
**Model default**: `Meta-Llama-3.3-70B-Instruct`

---

## 8. Forum Diskusi

### Struktur Forum

```
Thread (posting utama dengan judul)
├── Reply 1
│   └── Reply 1.1 (balasan ke reply 1)
│       └── Reply 1.1.1 (tidak ada batas kedalaman)
└── Reply 2
```

### Fitur Forum

- **Thread** — hanya bisa dibuat oleh user yang enrolled
- **Balasan** — bisa bersarang unlimited depth
- **Like** — setiap user bisa like sekali per post
- **Pin** — teacher/admin bisa pin thread (tampil paling atas)
- **Edit** — user bisa edit post sendiri
- **Hapus** — teacher/admin bisa hapus post siapapun
- **Mention** — `@username` untuk mention user lain
- **Soft delete** — post yang dihapus ditandai `IsDeleted`, tidak benar-benar dihapus dari DB

---

## 9. Progress & Sertifikat

### Tracking Progress

Sistem otomatis melacak progress setiap mahasiswa:
- `ModuleProgress` — apakah modul sudah selesai
- `CourseProgress` — persentase dari total modul

Progress diupdate saat mahasiswa menandai modul sebagai selesai.

### Aturan Penyelesaian Kursus

Teacher bisa mengatur aturan via `CourseCompletionRule` (disimpan sebagai JSON):
- Modul yang wajib diselesaikan
- Quiz yang harus dilewati
- Nilai minimum untuk lulus

### Penerbitan Sertifikat

Sertifikat diterbitkan otomatis saat semua aturan penyelesaian terpenuhi:
1. Sistem cek `CourseCompletionRule`
2. Semua terpenuhi → buat record `Certificate`
3. Nomor sertifikat unik digenerate
4. Notifikasi dikirim ke mahasiswa

**Verifikasi sertifikat** (public):
```
GET /api/certificates/verify/{nomor-sertifikat}
```
Bisa diakses tanpa login untuk memverifikasi keaslian.

---

## 10. Notifikasi

### Tipe Notifikasi

| Tipe | Trigger |
|------|---------|
| `Info` | Informasi umum |
| `Success` | Aksi berhasil |
| `Grade` | Nilai tugas/quiz keluar |
| `Assignment` | Tugas baru atau H-1 deadline |
| `Announcement` | Pengumuman baru dari kursus |
| `Quiz` | Quiz baru dipublish |
| `Certificate` | Sertifikat diterbitkan |

### Cara Kerja

Notifikasi dibuat oleh `INotificationService` dipanggil dari action:
- Enrollment → `CreateForEnrollmentAsync`
- Nilai keluar → `CreateForGradeAsync`
- Quiz dipublish → `CreateForQuizAvailableAsync`
- Due date H-1 → `CreateForAssignmentDueSoonAsync` (idempoten — tidak duplikat)
- Sertifikat → `CreateForCertificateAsync`

Frontend polling `/api/notifications/unread-count` untuk update badge.

---

## 11. Pesan Langsung (Direct Message)

- Percakapan 1:1 antar user
- Satu percakapan per pasang user (unique pair)
- Read receipt: tandai pesan sudah dibaca
- Diakses dari menu **Pesan**

---

## 12. Kalender

- Event terkait kursus (tugas due, quiz, pengumuman)
- Filter by range tanggal
- Teacher bisa buat event kustom
- Tampilan bulanan/mingguan

---

## 13. Kehadiran (Attendance)

- Teacher buat sesi kehadiran untuk kursus
- Tandai mahasiswa hadir/absen per sesi
- Export laporan kehadiran ke CSV/Excel

---

## 14. Admin Panel

### User Management

- Lihat semua user yang terdaftar
- Ubah role (student ↔ teacher ↔ admin)
- Lihat aktivitas user

### Course Management

- Lihat semua kursus di sistem
- Toggle publikasi kursus
- Hapus kursus (cascade — semua data ikut terhapus)

### Dashboard Admin

Menampilkan:
- Total user per role
- Total kursus
- Total enrollment
- Aktivitas terbaru
- User paling aktif

---

## 15. Pencarian Global

```
GET /api/search?q=kata+kunci&type=courses
```

| `type` | Cari di mana |
|--------|-------------|
| `courses` | Judul dan deskripsi kursus |
| `assignments` | Judul tugas |
| `forum` | Thread dan body forum post |
| (kosong) | Semua kategori |

---

## 16. Ekspor Laporan

| Laporan | Format | Endpoint |
|---------|--------|---------|
| Nilai kursus | CSV/Excel | GET `/api/reports/courses/{id}/grades` |
| Kehadiran | CSV/Excel | GET `/api/reports/courses/{id}/attendance` |
| Progress mahasiswa | CSV/Excel | GET `/api/reports/courses/{id}/progress` |
| Hasil question set | CSV/Excel | GET `/api/ujian/{id}/export` |
| Hasil mandatory exam | CSV/Excel | GET `/api/mandatory-exams/{id}/export` |
| Audit trail sesi token | CSV/Excel | (via dashboard manage ujian wajib) |

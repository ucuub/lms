# 01 — Gambaran Umum Proyek

## Apa Itu LMS Ini?

LMS (Learning Management System) adalah aplikasi web full-stack yang dibangun untuk mengelola kegiatan belajar-mengajar secara digital. Sistem ini mendukung alur lengkap dari pembuatan kursus oleh instruktur, pendaftaran mahasiswa, pelaksanaan ujian, penilaian, hingga penerbitan sertifikat.

Sistem ini juga terintegrasi dengan **DWI Mobile** (aplikasi mobile) melalui token bridge dan sistem ujian wajib berbasis deep-link.

---

## Tech Stack

### Backend

| Komponen | Teknologi |
|----------|-----------|
| Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 8.0 |
| Database (prod) | PostgreSQL |
| Database (dev) | SQL Server / SQLite |
| Auth | Keycloak JWT (JwtBearer) |
| Dokumentasi API | Swagger / OpenAPI |
| Export | CsvHelper, DocumentFormat.OpenXml |
| AI Integration | DekaLLM (OpenAI-compatible API) |

### Frontend

| Komponen | Teknologi |
|----------|-----------|
| Framework | Vue 3 (Composition API + `<script setup>`) |
| State Management | Pinia |
| Routing | Vue Router 4 |
| HTTP Client | Axios |
| Styling | Tailwind CSS |
| Build Tool | Vite |
| Auth Client | Keycloak.js |

---

## Struktur Folder Proyek

```
lms-new/
├── src/
│   └── LmsApp/                     # Backend ASP.NET Core
│       ├── Controllers/            # API Controllers (endpoint HTTP)
│       ├── Models/                 # Entity Framework Core models
│       ├── DTOs/                   # Data Transfer Objects
│       ├── Services/               # Logika bisnis
│       ├── Data/
│       │   ├── LmsDbContext.cs     # EF Core DbContext
│       │   └── DataSeeder.cs       # Seed data awal
│       ├── Program.cs              # Entry point + middleware
│       ├── appsettings.json        # Konfigurasi production
│       ├── appsettings.Development.json
│       └── LmsApp.csproj
│
├── frontend/                       # Frontend Vue 3
│   ├── src/
│   │   ├── views/                  # Halaman-halaman (37 view)
│   │   ├── components/             # Komponen reusable
│   │   ├── api/                    # Modul Axios API (16 file)
│   │   ├── stores/                 # Pinia stores
│   │   ├── router/                 # Konfigurasi Vue Router
│   │   ├── auth/                   # Keycloak + token store
│   │   ├── composables/            # Vue composables
│   │   ├── main.js
│   │   └── App.vue
│   ├── .env                        # Env production
│   ├── .env.development            # Env dev (mock auth)
│   ├── vite.config.js
│   └── package.json
│
├── docs/                           # Dokumentasi (folder ini)
├── LmsApp.sln                      # Visual Studio solution
└── docker-compose.yml
```

---

## Peran Pengguna

| Role | Akses |
|------|-------|
| `student` | Mendaftar kursus, mengikuti ujian, submit tugas, forum |
| `teacher` | Membuat kursus, mengelola konten, menilai, membuat ujian |
| `admin` | Semua akses teacher + manajemen pengguna, pengaturan sistem |

Role dikelola di database LMS (`AppUser.Role`), bukan di Keycloak — sehingga admin LMS bisa mengubah role tanpa perlu akses ke Keycloak admin panel.

---

## Daftar Fitur Utama

### Manajemen Kursus
- Buat, edit, publikasi, dan hapus kursus
- Organisasi konten dengan seksi dan modul
- Konten teks, video (YouTube/Vimeo), dan lampiran file
- Prasyarat kursus (blocking atau peringatan)
- Aturan penyelesaian kursus (configurable)

### Sistem Ujian (4 Jenis)
- **Quiz** — Ujian per-kursus (soal pilihan ganda, benar/salah, esai)
- **Practice Quiz** — Latihan mandiri tanpa perlu terdaftar di kursus
- **Question Set (Ujian)** — Ujian formal buatan instruktur, bisa diimpor dari bank soal
- **Mandatory Exam** — Ujian wajib dengan sistem deep-link (integrasi DWI Mobile)

### Tugas & Penilaian
- Buat tugas dengan batas waktu
- Upload file jawaban oleh mahasiswa
- Penilaian + feedback dari instruktur
- Buku nilai (gradebook) dengan bobot nilai

### Forum & Kolaborasi
- Forum diskusi per-kursus
- Balasan bersarang (unlimited depth)
- Sistem like, pin thread, mention (@user)

### Komunikasi
- Pesan langsung (direct message) antar pengguna
- Notifikasi sistem (nilai, pengumuman, quiz tersedia, dll.)

### Kemajuan & Sertifikat
- Pelacakan kemajuan per modul
- Penerbitan sertifikat otomatis saat kursus selesai
- Dashboard kemajuan mahasiswa

### Integrasi
- **Keycloak** — SSO identity provider
- **DWI Mobile** — Token bridge + deep-link ujian
- **DekaLLM** — Generasi soal otomatis dengan AI
- **Webhook** — Callback saat ujian wajib selesai

### Admin & Laporan
- Manajemen pengguna
- Log aktivitas sistem
- Export nilai / hasil ujian ke CSV / Excel
- Laporan kehadiran

---

## Alur Singkat Sistem

```
[Mahasiswa] ─── mendaftar ──► [Kursus]
                                  │
                          ┌───────┴───────┐
                       [Modul]         [Quiz]
                          │                │
                    [Progress]       [Attempt]
                          │                │
                    [Selesai] ────────► [Sertifikat]

[Instruktur] ─── buat ──► [Kursus] ──► [Bank Soal]
                                            │
                                      [Question Set]
                                            │
                                       [Gradebook]
```

# Dokumentasi LMS (Learning Management System)

Selamat datang di dokumentasi lengkap proyek LMS. Dokumentasi ini mencakup seluruh aspek sistem mulai dari arsitektur, database, API, frontend, hingga panduan deployment.

---

## Daftar Isi

| No | File | Deskripsi |
|----|------|-----------|
| 1 | [01-overview.md](01-overview.md) | Gambaran umum proyek, fitur, dan tech stack |
| 2 | [02-getting-started.md](02-getting-started.md) | Cara setup dan menjalankan project lokal |
| 3 | [03-architecture.md](03-architecture.md) | Arsitektur sistem backend dan frontend |
| 4 | [04-database.md](04-database.md) | Skema database, tabel, relasi |
| 5 | [05-api-reference.md](05-api-reference.md) | Referensi lengkap semua endpoint API |
| 6 | [06-authentication.md](06-authentication.md) | Sistem autentikasi (Keycloak, Mock, DWI Mobile Bridge) |
| 7 | [07-frontend.md](07-frontend.md) | Arsitektur frontend: router, store, komponen, API layer |
| 8 | [08-features.md](08-features.md) | Panduan fitur-fitur sistem |
| 9 | [09-configuration.md](09-configuration.md) | Konfigurasi backend dan frontend |
| 10 | [10-deployment.md](10-deployment.md) | Panduan deployment ke production |
| 11 | [11-security.md](11-security.md) | Keamanan, validasi token, dan best practices |

---

## Quick Start

### Backend
```cmd
cd src\LmsApp
dotnet run --launch-profile Development
```
Backend berjalan di: `http://localhost:5000`

### Frontend
```cmd
cd frontend
npm run dev
```
Frontend berjalan di: `http://localhost:5173`

---

## Stack Utama

- **Backend**: ASP.NET Core 8.0 (C#) + Entity Framework Core
- **Frontend**: Vue 3 + Pinia + Tailwind CSS + Vite
- **Database**: PostgreSQL (prod) / SQL Server (dev) / SQLite (opsional)
- **Auth**: Keycloak (JWT) + Mock Auth (dev) + DWI Mobile Bridge

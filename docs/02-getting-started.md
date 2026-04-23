# 02 — Getting Started (Cara Menjalankan Project)

## Prasyarat

| Tools | Versi Minimum |
|-------|--------------|
| .NET SDK | 8.0 |
| Node.js | 18.x |
| npm | 9.x |
| SQL Server | 2019+ (untuk development) |
| PostgreSQL | 14+ (untuk production) |

---

## Cara Menjalankan Secara Lokal

### 1. Clone atau Buka Project

Pastikan kamu berada di root folder project:
```
C:\Users\yusuf4\Desktop\lms-new\
```

---

### 2. Jalankan Backend

Buka CMD pertama:

```cmd
cd C:\Users\yusuf4\Desktop\lms-new\src\LmsApp
dotnet run --launch-profile Development
```

Backend akan berjalan di: **http://localhost:5000**

Swagger UI tersedia di: **http://localhost:5000/swagger**

> **Catatan**: Backend Development menggunakan SQL Server (`PHMSQLDEV01\DEVSQL01`, database `WCI_APP`). Pastikan server tersebut bisa diakses.

Jika menggunakan `dotnet.exe` secara eksplisit:
```cmd
"C:\Program Files\dotnet\dotnet.exe" run --launch-profile Development
```

Cek lokasi dotnet.exe:
```cmd
where dotnet
```

---

### 3. Jalankan Frontend

Buka CMD kedua (terpisah):

```cmd
cd C:\Users\yusuf4\Desktop\lms-new\frontend
npm install
npm run dev
```

Frontend akan berjalan di: **http://localhost:5173**

---

## Konfigurasi Database

Backend otomatis mendeteksi jenis database dari connection string:

| Pola Connection String | Database |
|------------------------|----------|
| Dimulai dengan `Data Source` | SQLite |
| Mengandung `Server=` / `Initial Catalog=` / `Trusted_Connection=` | SQL Server |
| Selain itu | PostgreSQL |

### Untuk SQLite (Paling Mudah untuk Dev)

Edit `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=lms_dev.db"
  }
}
```

Jalankan backend — database SQLite akan dibuat otomatis.

### Untuk SQL Server

Connection string sudah dikonfigurasi di `appsettings.Development.json`:
```json
"DefaultConnection": "Server=PHMSQLDEV01\\DEVSQL01;Database=WCI_APP;User Id=wciappusr;Password=wciapppwd;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

### Untuk PostgreSQL

Edit connection string:
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=lms;Username=lmsuser;Password=lmspassword"
```

---

## Seed Data Awal

Saat pertama kali backend dijalankan, `DataSeeder.SeedAsync()` otomatis berjalan dan membuat:
- 2 teacher (user demo)
- 4 student (user demo)
- Beberapa kursus contoh dengan modul, quiz, dan tugas

Data seed hanya berjalan jika belum ada data (idempoten).

---

## Mock Auth (Development)

File `.env.development` sudah mengaktifkan mock auth:
```
VITE_MOCK_AUTH=true
VITE_API_URL=/api
```

Dengan mock auth aktif:
- **Tidak perlu Keycloak** untuk login
- Login otomatis sebagai `student1` (default)
- Gunakan komponen **MockUserSwitcher** di UI untuk ganti user/role
- Frontend mengirim header: `X-Mock-User-Id`, `X-Mock-User-Role`, `X-Mock-User-Name`
- Backend (mode Development) membaca header tersebut tanpa validasi JWT

---

## Proxy Frontend ke Backend

`vite.config.js` sudah mengatur proxy:
```javascript
proxy: {
  '/api': { target: 'http://localhost:5000', changeOrigin: true },
  '/uploads': { target: 'http://localhost:5000', changeOrigin: true }
}
```

Artinya semua request `/api/...` dari frontend otomatis diteruskan ke backend — tidak ada CORS issue.

---

## Urutan Menjalankan

1. Pastikan database (SQL Server / SQLite / PostgreSQL) bisa diakses
2. Jalankan **backend** dulu (`dotnet run`)
3. Setelah backend up (muncul pesan "Now listening on: http://localhost:5000"), baru jalankan **frontend** (`npm run dev`)
4. Buka browser: `http://localhost:5173`

---

## Troubleshooting Umum

### Backend Gagal Start

**Error: Cannot open database**
- Cek connection string di `appsettings.Development.json`
- Pastikan SQL Server berjalan dan bisa diakses

**Error: Address already in use (port 5000)**
```cmd
netstat -ano | findstr :5000
taskkill /PID <pid> /F
```

### Frontend Gagal Start

**Error: Cannot find module**
```cmd
cd frontend
npm install
```

**Error: EACCES / permission denied**
- Jalankan CMD sebagai Administrator

### Request API Gagal (Network Error)
- Pastikan backend berjalan di port 5000
- Cek proxy di `vite.config.js`

### Token / Auth Error di Backend
- Akses `http://localhost:5000/api/auth/debug-claims` (development only) untuk melihat claims JWT yang masuk

---

## Scripts Yang Tersedia

### Backend
```cmd
dotnet run                          # Run development
dotnet run --launch-profile http    # Run with http profile
dotnet build                        # Build only
dotnet build -c Release             # Release build
dotnet ef migrations add NamaMigration   # Tambah migration
dotnet ef database update                # Apply migrations
```

### Frontend
```cmd
npm run dev      # Dev server (port 5173)
npm run build    # Build production ke folder dist/
npm run preview  # Preview hasil build
```

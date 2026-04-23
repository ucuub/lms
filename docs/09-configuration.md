# 09 — Konfigurasi

## Backend Configuration

### File Konfigurasi

| File | Keterangan |
|------|-----------|
| `appsettings.json` | Konfigurasi default / production |
| `appsettings.Development.json` | Override untuk environment Development |
| `appsettings.Production.json` | Override untuk environment Production (dibuat manual) |

File dengan environment lebih spesifik **menimpa** nilai dari file lebih umum.

---

### `appsettings.json` (Production Default)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=lms;Username=lmsuser;Password=lmspassword"
  },

  "Keycloak": {
    "Authority": "http://localhost:8080/realms/lms",
    "ClientId": "lms-app",
    "AllowedClientId": "dwi-mobile",
    "RequireHttpsMetadata": false
  },

  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  },

  "FileStorage": {
    "BasePath": "wwwroot/uploads"
  },

  "MandatoryExam": {
    "SigningKey": "mandatory-exam-default-key-CHANGE-IN-PRODUCTION-min32chars!",
    "FrontendBaseUrl": "http://localhost:5173"
  },

  "ServiceIntegration": {
    "ApiKey": "CHANGE-THIS-TO-A-STRONG-RANDOM-SECRET-IN-PRODUCTION"
  },

  "LLM": {
    "ApiKey": "",
    "Model": "Meta-Llama-3.3-70B-Instruct",
    "BaseUrl": "https://dekallm.cloudeka.ai/v1",
    "ProviderName": "DekaLLM"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  },

  "AllowedHosts": "*"
}
```

---

### `appsettings.Development.json` (Development Override)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=PHMSQLDEV01\\DEVSQL01;Database=WCI_APP;User Id=wciappusr;Password=wciapppwd;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },

  "Keycloak": {
    "Authority": "http://localhost:8080/realms/lms",
    "ClientId": "lms-app",
    "AllowedClientId": "lms-app",
    "RequireHttpsMetadata": false
  },

  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  },

  "ServiceIntegration": {
    "ApiKey": "dev-dwi-mobile-api-key-local"
  },

  "LLM": {
    "ApiKey": "sk-xxxx",
    "Model": "meta/llama-4-maverick-instruct",
    "BaseUrl": "https://dekallm.cloudeka.ai/v1",
    "ProviderName": "DekaLLM"
  },

  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
    }
  }
}
```

---

### Penjelasan Setiap Section

#### ConnectionStrings

| Key | Keterangan |
|-----|-----------|
| `DefaultConnection` | Connection string database utama. Format otomatis terdeteksi |

**Format per database**:

SQLite:
```
Data Source=lms_dev.db
```

SQL Server:
```
Server=NAMA_SERVER;Database=NAMA_DB;User Id=USER;Password=PASS;TrustServerCertificate=True
```

PostgreSQL:
```
Host=localhost;Port=5432;Database=lms;Username=lmsuser;Password=lmspassword
```

---

#### Keycloak

| Key | Keterangan |
|-----|-----------|
| `Authority` | URL realm Keycloak. Format: `{keycloak-url}/realms/{realm-name}` |
| `ClientId` | Client ID yang harus ada di klaim `aud` token |
| `AllowedClientId` | Client ID yang diizinkan di klaim `azp`. Dev: `lms-app`, Prod: `dwi-mobile` |
| `RequireHttpsMetadata` | `true` untuk production. `false` untuk HTTP (dev) |

---

#### Cors

| Key | Keterangan |
|-----|-----------|
| `AllowedOrigins` | Array URL origin yang diizinkan. Isi dengan domain frontend production |

Contoh production:
```json
"AllowedOrigins": [
  "https://lms.namainstansi.com",
  "https://app.dwidigital.id"
]
```

---

#### FileStorage

| Key | Keterangan |
|-----|-----------|
| `BasePath` | Direktori penyimpanan file upload, relatif dari root aplikasi |

Default: `wwwroot/uploads` — file bisa diakses via `/uploads/{nama-file}`

---

#### MandatoryExam

| Key | Keterangan |
|-----|-----------|
| `SigningKey` | Secret key untuk JWT token ujian wajib (HS256). **Minimum 32 karakter!** |
| `FrontendBaseUrl` | URL frontend — dipakai untuk generate deep-link ujian |

> **PENTING**: Ganti `SigningKey` dengan nilai random yang kuat di production. Jika bocor, orang bisa memalsukan token ujian.

Generate key yang aman:
```cmd
dotnet user-secrets set "MandatoryExam:SigningKey" "$(openssl rand -base64 48)"
```

---

#### ServiceIntegration

| Key | Keterangan |
|-----|-----------|
| `ApiKey` | API key untuk integrasi DWI Mobile — validasi request dari mobile |

---

#### LLM (AI Integration)

| Key | Keterangan |
|-----|-----------|
| `ApiKey` | API key provider LLM |
| `Model` | Nama model LLM yang digunakan |
| `BaseUrl` | Base URL API (OpenAI-compatible format) |
| `ProviderName` | Nama provider (hanya untuk label) |

Sistem menggunakan OpenAI-compatible API, sehingga bisa diganti ke provider lain:
- OpenAI: `https://api.openai.com/v1`
- Azure OpenAI: URL endpoint Azure
- DekaLLM: `https://dekallm.cloudeka.ai/v1`

---

#### Logging

| Level | Keterangan |
|-------|-----------|
| `Information` | Info umum (request masuk, actions) |
| `Warning` | Peringatan non-fatal |
| `Error` | Error yang perlu diperhatikan |
| `Debug` | Detail untuk debugging (verbose, hindari di prod) |

---

### Environment Variables Override

Semua nilai `appsettings.json` bisa di-override via environment variables menggunakan format:

```
SECTION__KEY=value
```

Contoh:
```
ConnectionStrings__DefaultConnection=Host=prod-db;Database=lms;...
Keycloak__Authority=https://auth.namainstansi.com/realms/lms
MandatoryExam__SigningKey=super-secret-key-production-only
```

Gunakan ini untuk Docker/Kubernetes deployment tanpa menyimpan secret di file.

---

## Frontend Configuration

### File Environment

| File | Keterangan |
|------|-----------|
| `.env` | Default untuk semua mode |
| `.env.development` | Override saat `npm run dev` |
| `.env.production` | Override saat `npm run build` |
| `.env.local` | Override lokal, tidak di-commit ke git |

---

### Variabel yang Tersedia

| Variabel | Keterangan |
|----------|-----------|
| `VITE_API_URL` | Base URL backend API |
| `VITE_KEYCLOAK_URL` | URL server Keycloak |
| `VITE_KEYCLOAK_REALM` | Nama realm Keycloak |
| `VITE_KEYCLOAK_CLIENT_ID` | Client ID Keycloak |
| `VITE_DWI_MOBILE_URL` | URL server DWI Mobile (untuk token refresh bridge) |
| `VITE_MOCK_AUTH` | `true` untuk aktifkan mock auth (dev only) |

---

### Nilai per Mode

**Development (`.env.development`)**:
```env
VITE_API_URL=/api
VITE_MOCK_AUTH=true
```

`/api` → diproxy Vite ke `http://localhost:5000` (lihat `vite.config.js`)

**Production (`.env`)**:
```env
VITE_API_URL=http://localhost:5000/api
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=lms
VITE_KEYCLOAK_CLIENT_ID=lms-app
VITE_DWI_MOBILE_URL=http://localhost:8000
```

Ganti nilai-nilai di atas dengan URL production saat build.

---

### Contoh Konfigurasi Production Frontend

Buat file `.env.production.local` (tidak di-commit):
```env
VITE_API_URL=https://api.lms.namainstansi.com/api
VITE_KEYCLOAK_URL=https://auth.namainstansi.com
VITE_KEYCLOAK_REALM=lms
VITE_KEYCLOAK_CLIENT_ID=lms-app
VITE_DWI_MOBILE_URL=https://mobile-api.namainstansi.com
```

Lalu build:
```cmd
npm run build
```

---

## Launch Profiles Backend

File: `src/LmsApp/Properties/launchSettings.json`

```json
{
  "profiles": {
    "Development": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "http": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Gunakan profil dengan:
```cmd
dotnet run --launch-profile Development
dotnet run --launch-profile http
```

---

## Rate Limit Configuration

Dikonfigurasi langsung di `Program.cs` (hardcoded saat ini):

| Limit | Nilai |
|-------|-------|
| Global (per IP, per menit) | 100 request |
| Upload endpoint (per IP, per menit) | 10 request |

Untuk mengubah, edit `Program.cs`:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 100;          // ← ubah di sini
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("upload", opt =>
    {
        opt.PermitLimit = 10;           // ← ubah di sini
        opt.Window = TimeSpan.FromMinutes(1);
    });
});
```

---

## NuGet.Config

File `NuGet.Config` di root project mengatur sumber package NuGet. Jika berada di environment tanpa akses internet, konfigurasikan `packageSources` ke mirror internal.

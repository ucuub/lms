# 10 — Panduan Deployment

## Overview

Sistem ini terdiri dari tiga komponen yang perlu di-deploy:

1. **Backend** — ASP.NET Core 8.0 Web API
2. **Frontend** — Vue 3 (static files hasil build Vite)
3. **Database** — PostgreSQL (production)

Opsional:
- **Keycloak** — Identity Provider (bisa pakai hosted/cloud Keycloak)
- **Nginx / Reverse Proxy** — Untuk routing dan SSL termination

---

## Checklist Persiapan

### Backend
- [ ] Database PostgreSQL siap (atau SQL Server)
- [ ] Keycloak realm dan client sudah dikonfigurasi
- [ ] File `appsettings.Production.json` sudah dibuat dengan nilai yang benar
- [ ] `MandatoryExam:SigningKey` sudah diganti dengan secret yang kuat
- [ ] CORS `AllowedOrigins` sudah diisi dengan domain frontend production
- [ ] `ServiceIntegration:ApiKey` sudah diganti
- [ ] Folder `wwwroot/uploads` bisa ditulis oleh proses backend

### Frontend
- [ ] Semua `VITE_*` env vars sudah dikonfigurasi untuk production
- [ ] `VITE_MOCK_AUTH` tidak ada atau bernilai `false`
- [ ] `VITE_API_URL` menunjuk ke backend URL yang benar

---

## Deploy Backend

### 1. Build Release

```cmd
cd C:\Users\yusuf4\Desktop\lms-new\src\LmsApp
dotnet build -c Release
dotnet publish -c Release -o ./publish
```

Output akan ada di folder `./publish`.

### 2. Buat appsettings.Production.json

Buat file `appsettings.Production.json` di folder publish:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db.namainstansi.com;Port=5432;Database=lms_prod;Username=lmsuser;Password=PASSWORD_KUAT"
  },
  "Keycloak": {
    "Authority": "https://auth.namainstansi.com/realms/lms",
    "ClientId": "lms-app",
    "AllowedClientId": "dwi-mobile",
    "RequireHttpsMetadata": true
  },
  "Cors": {
    "AllowedOrigins": [
      "https://lms.namainstansi.com"
    ]
  },
  "FileStorage": {
    "BasePath": "/var/lms/uploads"
  },
  "MandatoryExam": {
    "SigningKey": "RANDOM-SECRET-MINIMUM-32-CHARS-PRODUCTION",
    "FrontendBaseUrl": "https://lms.namainstansi.com"
  },
  "ServiceIntegration": {
    "ApiKey": "RANDOM-API-KEY-PRODUCTION"
  },
  "LLM": {
    "ApiKey": "LLM-API-KEY",
    "Model": "Meta-Llama-3.3-70B-Instruct",
    "BaseUrl": "https://dekallm.cloudeka.ai/v1",
    "ProviderName": "DekaLLM"
  }
}
```

### 3. Jalankan Backend

```cmd
cd publish
set ASPNETCORE_ENVIRONMENT=Production
dotnet LmsApp.dll
```

Atau dengan URL custom:
```cmd
set ASPNETCORE_URLS=http://+:5000
dotnet LmsApp.dll
```

### 4. Jalankan sebagai Windows Service (Opsional)

```cmd
sc create LmsApi binpath="dotnet C:\inetpub\lms\LmsApp.dll" start=auto
sc start LmsApi
```

---

## Deploy Frontend

### 1. Set Environment Variables Production

Buat file `.env.production.local` di folder `frontend/` (tidak di-commit ke git):

```env
VITE_API_URL=https://api.lms.namainstansi.com/api
VITE_KEYCLOAK_URL=https://auth.namainstansi.com
VITE_KEYCLOAK_REALM=lms
VITE_KEYCLOAK_CLIENT_ID=lms-app
VITE_DWI_MOBILE_URL=https://mobile-api.namainstansi.com
```

### 2. Build

```cmd
cd C:\Users\yusuf4\Desktop\lms-new\frontend
npm install
npm run build
```

Output: folder `dist/`

### 3. Serve Static Files

#### Opsi A: Nginx

```nginx
server {
    listen 443 ssl;
    server_name lms.namainstansi.com;

    ssl_certificate /etc/ssl/certs/lms.crt;
    ssl_certificate_key /etc/ssl/private/lms.key;

    root /var/www/lms/dist;
    index index.html;

    # SPA routing — semua route ke index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Proxy API ke backend
    location /api/ {
        proxy_pass http://localhost:5000/api/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # Proxy uploads ke backend
    location /uploads/ {
        proxy_pass http://localhost:5000/uploads/;
    }
}
```

#### Opsi B: Serve langsung dari IIS

1. Publish `dist/` ke folder IIS
2. Tambahkan `web.config` untuk URL rewrite (SPA routing)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <rewrite>
      <rules>
        <rule name="SPA Fallback" stopProcessing="true">
          <match url=".*" />
          <conditions logicalGrouping="MatchAll">
            <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
            <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
          </conditions>
          <action type="Rewrite" url="/index.html" />
        </rule>
      </rules>
    </rewrite>
  </system.webServer>
</configuration>
```

---

## Setup Database PostgreSQL

### 1. Install PostgreSQL

```bash
# Ubuntu/Debian
sudo apt install postgresql postgresql-contrib

# Atau download installer untuk Windows dari postgresql.org
```

### 2. Buat Database dan User

```sql
-- Masuk sebagai postgres superuser
psql -U postgres

-- Buat user
CREATE USER lmsuser WITH PASSWORD 'PASSWORD_KUAT';

-- Buat database
CREATE DATABASE lms_prod OWNER lmsuser;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE lms_prod TO lmsuser;
```

### 3. Jalankan Backend Pertama Kali

Backend akan otomatis membuat tabel saat pertama kali dijalankan (`EnsureCreated()` atau migration).

Jika menggunakan EF Core Migrations:
```cmd
cd src\LmsApp
dotnet ef database update
```

---

## Setup Keycloak

### 1. Install Keycloak

Download dari [keycloak.org](https://www.keycloak.org/downloads) atau gunakan Docker:

```bash
docker run -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

### 2. Konfigurasi Realm

1. Buka `http://localhost:8080`
2. Login sebagai admin
3. Buat realm baru: **`lms`**

### 3. Buat Client `lms-app`

1. Clients → Create client
2. Client ID: `lms-app`
3. Client type: OpenID Connect
4. Settings:
   - Valid redirect URIs: `http://localhost:5173/*` (atau URL production)
   - Web origins: `http://localhost:5173` (atau URL production)
5. Capability config: Standard flow ✓

### 4. Buat Client `dwi-mobile`

1. Client ID: `dwi-mobile`
2. Client type: OpenID Connect
3. Client authentication: ON (confidential)
4. Tambahkan **Audience mapper** ke lms-app:
   - Mappers → Add mapper → Audience
   - Included Client Audience: `lms-app`
   - Add to access token: ON

### 5. Buat User

1. Users → Create user
2. Isi username, email, nama
3. Set password (Credentials tab)

### 6. Assign Role (Opsional via Keycloak)

Role di LMS dikelola sendiri di DB, tapi bisa juga assign via Keycloak realm roles untuk sinkronisasi awal.

---

## Docker Compose (All-in-One)

`docker-compose.yml` sudah tersedia di root project:

```bash
docker-compose up -d
```

Ini akan menjalankan:
- Backend (dari Dockerfile)
- Frontend (dari frontend/Dockerfile.frontend)
- PostgreSQL
- Keycloak (jika dikonfigurasi)

Pastikan environment variables sudah diset sebelum menjalankan.

---

## Reverse Proxy dengan SSL

Untuk production, selalu gunakan HTTPS. Contoh setup Nginx sebagai reverse proxy:

```nginx
# Redirect HTTP ke HTTPS
server {
    listen 80;
    server_name lms.namainstansi.com api.lms.namainstansi.com;
    return 301 https://$host$request_uri;
}

# Frontend
server {
    listen 443 ssl http2;
    server_name lms.namainstansi.com;

    ssl_certificate /etc/ssl/lms.crt;
    ssl_certificate_key /etc/ssl/lms.key;

    root /var/www/lms/dist;

    location / {
        try_files $uri /index.html;
    }
}

# Backend API
server {
    listen 443 ssl http2;
    server_name api.lms.namainstansi.com;

    ssl_certificate /etc/ssl/lms.crt;
    ssl_certificate_key /etc/ssl/lms.key;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto https;
    }
}
```

Jika menggunakan satu domain dengan path:
```nginx
server {
    listen 443 ssl;
    server_name lms.namainstansi.com;

    root /var/www/lms/dist;

    location /api/ {
        proxy_pass http://localhost:5000/api/;
    }

    location /uploads/ {
        proxy_pass http://localhost:5000/uploads/;
    }

    location / {
        try_files $uri /index.html;
    }
}
```

---

## Monitoring & Maintenance

### Log Backend

Logs ditulis ke stdout secara default. Untuk menyimpan ke file, konfigurasi di `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Health Check

Backend memiliki built-in health check yang bisa dipantau:

```
GET http://localhost:5000/health
```

### Backup Database

```bash
# PostgreSQL backup
pg_dump -U lmsuser lms_prod > lms_backup_$(date +%Y%m%d).sql

# Restore
psql -U lmsuser lms_prod < lms_backup_20241201.sql
```

### Update Aplikasi

1. Build ulang backend/frontend
2. Stop service
3. Copy file baru (jangan hapus folder `wwwroot/uploads` — ini berisi file upload user)
4. Start service

### File Uploads

File upload disimpan di `wwwroot/uploads/` (atau path yang dikonfigurasi di `FileStorage:BasePath`).

**Penting**: Sertakan folder ini dalam backup reguler dan **jangan hapus** saat update aplikasi.

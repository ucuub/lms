# 03 — Arsitektur Sistem

## Gambaran Arsitektur

```
┌─────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                          │
│                                                              │
│   Browser                    DWI Mobile App                 │
│   ┌────────────────┐         ┌──────────────┐               │
│   │  Vue 3 (Vite)  │         │ Mobile App   │               │
│   │  port :5173    │         │              │               │
│   └────────┬───────┘         └──────┬───────┘               │
└────────────┼─────────────────────────┼─────────────────────-┘
             │  HTTP / REST            │  Token Exchange
             │  (proxy via Vite)       │
┌────────────┼─────────────────────────┼──────────────────────┐
│            ▼          BACKEND LAYER  ▼                       │
│   ┌─────────────────────────────────────────────┐           │
│   │         ASP.NET Core 8.0 (:5000)            │           │
│   │                                             │           │
│   │  ┌──────────┐  ┌──────────┐  ┌──────────┐  │           │
│   │  │Controllers│  │ Services │  │  DTOs    │  │           │
│   │  └──────────┘  └──────────┘  └──────────┘  │           │
│   │  ┌──────────────────────────────────────┐   │           │
│   │  │      Entity Framework Core 8.0       │   │           │
│   │  └──────────────────────────────────────┘   │           │
│   └──────────────────────────┬──────────────────┘           │
└─────────────────────────────-┼──────────────────────────────┘
                               │
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
       ┌──────────┐    ┌──────────┐    ┌──────────────┐
       │PostgreSQL│    │SQL Server│    │   SQLite     │
       │ (prod)   │    │  (dev)   │    │  (opsional)  │
       └──────────┘    └──────────┘    └──────────────┘

              ┌────────────────┐
              │    Keycloak    │ ◄── SSO Identity Provider
              │ (:8080)        │
              └────────────────┘
```

---

## Backend Architecture

### Middleware Pipeline

Request HTTP masuk diproses secara berurutan:

```
[Request Masuk]
      │
      ▼
ExceptionHandlerMiddleware     ← Tangkap semua exception, return 500
      │
      ▼
RateLimiterMiddleware          ← 100 req/mnt per IP (10 req/mnt untuk upload)
      │
      ▼
CorsMiddleware                 ← Whitelist origin yang diizinkan
      │
      ▼
ForwardedHeadersMiddleware     ← Parsing X-Forwarded-For (behind proxy/LB)
      │
      ▼
StaticFilesMiddleware          ← Serve /uploads/* dari wwwroot
      │
      ▼
AuthenticationMiddleware       ← Validasi JWT (Keycloak)
      │
      ▼
MockAuthMiddleware             ← (Dev only) Baca header X-Mock-*
      │
      ▼
AuthorizationMiddleware        ← Cek role dan policy
      │
      ▼
[Controller Action]
```

### Lapisan Backend

#### 1. Controllers (`/Controllers`)
- Menerima HTTP request
- Validasi input dasar
- Panggil Services
- Return response DTO

#### 2. Services (`/Services`)
- Logika bisnis utama
- Akses database via `LmsDbContext`
- Dipanggil oleh controllers
- Di-inject via DI container

#### 3. Models (`/Models`)
- Entity Framework Core entities
- Merepresentasikan tabel database
- Navigation properties untuk relasi

#### 4. DTOs (`/DTOs`)
- `*Request` — input dari client
- `*Response` — output ke client
- Memisahkan model DB dari kontrak API

#### 5. Data (`/Data`)
- `LmsDbContext` — konfigurasi EF Core, DbSet, relationship
- `DataSeeder` — data awal untuk dev/demo

### Dependency Injection

Semua service didaftarkan di `Program.cs`:

```csharp
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IForumService, ForumService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<MandatoryExamTokenService>();
builder.Services.AddScoped<AiQuestionService>();
builder.Services.AddScoped<MaterialContextService>();
// ...dan lainnya
```

---

## Frontend Architecture

### Struktur Folder Frontend

```
frontend/src/
├── main.js                  # Entry point, mount Vue app
├── App.vue                  # Root component
├── router/
│   └── index.js             # Vue Router: semua route definitions
├── stores/
│   ├── auth.js              # Pinia store: user session
│   └── notifications.js     # Pinia store: notifikasi
├── auth/
│   ├── keycloak.js          # Keycloak.js adapter
│   └── tokenStore.js        # Token storage (memory + sessionStorage)
├── api/
│   ├── axios.js             # Axios instance + interceptor auth
│   ├── auth.js              # Auth API calls
│   ├── courses.js           # Course API calls
│   ├── quizzes.js           # Quiz API calls
│   ├── assignments.js       # Assignment API calls
│   ├── questionSets.js      # Question set API calls
│   ├── exams.js             # Standalone exam API calls
│   ├── practice.js          # Practice quiz API calls
│   ├── mandatoryExam.js     # Mandatory exam API calls
│   ├── forum.js             # Forum API calls
│   ├── messages.js          # Messaging API calls
│   ├── notifications.js     # Notification API calls
│   ├── dashboard.js         # Dashboard API calls
│   ├── gradebook.js         # Gradebook API calls
│   ├── activity.js          # Activity log API calls
│   ├── search.js            # Search API calls
│   ├── ai.js                # AI generation API calls
│   └── courseQuestionBank.js # Course question bank
├── views/
│   ├── auth/                # Login, access denied
│   ├── courses/             # Browse, detail, create, edit, question bank
│   ├── modules/             # View, create, edit modul
│   ├── assignments/         # View tugas, submission
│   ├── quiz/                # Quiz, attempt, result, manage
│   ├── exam/                # Question set, mandatory exam
│   ├── practice/            # Practice quiz
│   ├── forum/               # Thread, diskusi
│   ├── gradebook/           # Buku nilai
│   ├── certificates/        # Sertifikat
│   ├── calendar/            # Kalender
│   ├── notifications/       # Notifikasi
│   ├── messages/            # Pesan langsung
│   ├── activity/            # Log aktivitas
│   ├── admin/               # Panel admin
│   └── DashboardView.vue    # Dashboard utama
├── components/
│   ├── AppLayout.vue        # Shell utama (sidebar, header)
│   ├── CourseCard.vue       # Card kursus
│   ├── AlertMessage.vue     # Alert/toast
│   ├── Pagination.vue       # Komponen paginasi
│   ├── MockUserSwitcher.vue # Dev-only: ganti user mock
│   ├── ToastContainer.vue   # Toast notifications
│   ├── SearchBar.vue        # Search global
│   └── AiGenerateModal.vue  # Modal generasi soal AI
└── composables/
    └── useToast.js          # Composable toast helper
```

### Alur Data Frontend

```
[User Action]
     │
     ▼
[View Component]   ← memanggil
     │
     ▼
[API Module]       ← axios.get/post/put/delete
     │
     ▼
[Axios Interceptor] ← inject Authorization header
     │
     ▼
[HTTP Request]     → Backend API
     │
     ◄ HTTP Response
     │
[View Component]   ← update state lokal / Pinia store
     │
     ▼
[UI Update]
```

---

## Integrasi Keycloak

```
[Browser]
    │
    │  1. Cek sesi (check-sso)
    ▼
[Keycloak Server]
    │
    │  2. Redirect ke login jika belum login
    │  3. Return JWT access token + refresh token
    ▼
[Browser menyimpan token]
    │
    │  4. Setiap request: inject Authorization: Bearer {token}
    ▼
[Backend]
    │
    │  5. JwtBearerMiddleware: validasi signature, issuer, audience, expiry
    │  6. OnTokenValidated: cek azp claim
    │  7. AppUserClaimsTransformation: inject DB role ke principal
    ▼
[Controller Action]
```

---

## Integrasi DWI Mobile (Bridge)

```
[DWI Mobile App]
    │
    │  1. Arahkan user ke LMS URL dengan ?_lms_auth=CODE
    ▼
[LMS Frontend]
    │
    │  2. Deteksi param _lms_auth, kirim code ke backend
    ▼
[Backend /api/auth/lms-exchange]
    │
    │  3. Validasi code ke DWI Mobile server
    │  4. Return: { access_token, refresh_token, expires_in }
    ▼
[tokenStore.js]
    │
    │  5. Simpan: access_token di memory, refresh_token di sessionStorage
    │  6. Setiap request: inject Bearer token dari memory
    │  7. Jika expired: refresh otomatis via DWI Mobile refresh endpoint
    ▼
[Normal LMS Usage]
```

---

## Mandatory Exam Deep-Link Flow

```
[Admin/DWI Mobile]
    │
    │  1. Generate deep-link via POST /api/mandatory-exams/{id}/generate-link
    ▼
[Backend]
    │
    │  2. Buat JWT token (HS256, signed dengan MandatoryExam:SigningKey)
    │  3. Token berisi: exam_id, sub (userId/null), jti, exp
    │  4. Simpan sesi ke MandatoryExamSession (audit trail)
    │  5. Return URL: {FrontendBaseUrl}/exam/start?token={token}
    ▼
[Frontend /exam/start]
    │
    │  6. Kirim token ke backend via X-Exam-Token header
    │  7. Backend validasi token: signature, expiry, revocation
    │  8. Assign exam ke user, buat attempt
    ▼
[Ujian Berlangsung]
    │
    │  9. Submit jawaban
    │  10. Backend hitung skor
    │  11. Trigger webhook ke DWI Mobile (jika dikonfigurasi)
    ▼
[Selesai]
```

---

## Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| Semua endpoint | 100 req/menit per IP | Fixed 1 menit |
| Upload endpoint | 10 req/menit per IP | Fixed 1 menit |

Response saat limit terlewati:
```json
HTTP 429 Too Many Requests
{ "message": "Terlalu banyak request. Coba lagi nanti." }
```

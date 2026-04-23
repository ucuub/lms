# 06 — Sistem Autentikasi

## Overview

Sistem ini mendukung **3 jalur autentikasi**:

| Jalur | Kapan Digunakan |
|-------|----------------|
| **Mock Auth** | Development lokal (tanpa Keycloak) |
| **Keycloak JWT** | Production dan staging |
| **DWI Mobile Bridge** | Integrasi aplikasi mobile |

---

## Jalur 1: Mock Auth (Development Only)

### Cara Kerja

```
Frontend (.env.development: VITE_MOCK_AUTH=true)
    │
    │  Request dengan header:
    │  X-Mock-User-Id: student1
    │  X-Mock-User-Role: student
    │  X-Mock-User-Name: Student 1
    ▼
Backend (ASPNETCORE_ENVIRONMENT=Development)
    │
    │  MockAuthMiddleware mendeteksi header
    │  Buat ClaimsPrincipal sintetis (tanpa validasi JWT)
    ▼
Controller (berjalan seolah user sudah login)
```

### Konfigurasi Frontend

**`.env.development`**:
```
VITE_MOCK_AUTH=true
VITE_API_URL=/api
```

**`axios.js` interceptor**:
```javascript
if (import.meta.env.VITE_MOCK_AUTH === 'true') {
  const mockUser = JSON.parse(localStorage.getItem('mockUser'))
    ?? { id: 'student1', role: 'student', name: 'Student 1' }
  config.headers['X-Mock-User-Id']   = mockUser.id
  config.headers['X-Mock-User-Role'] = mockUser.role
  config.headers['X-Mock-User-Name'] = mockUser.name
  return config
}
```

### Ganti User Mock

Data user mock disimpan di `localStorage` sebagai `mockUser`:
```json
{ "id": "teacher1", "role": "teacher", "name": "Teacher 1" }
```

Gunakan komponen **MockUserSwitcher** di UI untuk ganti user tanpa mengubah kode.

### Batasan Mock Auth

- Hanya aktif di environment Development
- Backend harus dijalankan dengan `ASPNETCORE_ENVIRONMENT=Development`
- **Tidak boleh aktif di production** — tidak ada validasi keamanan apapun

---

## Jalur 2: Keycloak JWT (Production)

### Alur Lengkap

```
1. User buka aplikasi
         │
         ▼
2. Keycloak.js init (check-sso)
   → Cek apakah ada sesi Keycloak aktif
         │
   ┌─────┴─────┐
   │           │
Sudah       Belum
login       login
   │           │
   │       Redirect ke Keycloak login page
   │           │
   └─────┬─────┘
         │
3. Keycloak return JWT tokens
   - access_token (5 menit)
   - refresh_token (24 jam)
         │
         ▼
4. Axios interceptor inject token
   Authorization: Bearer {access_token}
         │
         ▼
5. Backend: JwtBearerMiddleware
   a. Fetch OIDC discovery dari Authority
   b. Validasi signature JWT dengan JWKS
   c. Validasi issuer = Authority
   d. Validasi audience = "lms-app"
   e. Validasi lifetime (± 30 detik clock skew)
         │
         ▼
6. OnTokenValidated callback
   a. Cek claim azp = "dwi-mobile" (configurable)
   b. Cek claim sub tidak kosong
         │
         ▼
7. AppUserClaimsTransformation
   a. Query AppUser dari DB berdasarkan UserId (sub)
   b. Inject DB role sebagai claim "role"
         │
         ▼
8. Controller menerima request dengan user terautentikasi
```

### Konfigurasi Backend (appsettings.json)

```json
{
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/lms",
    "ClientId": "lms-app",
    "AllowedClientId": "dwi-mobile",
    "RequireHttpsMetadata": false
  }
}
```

| Parameter | Keterangan |
|-----------|-----------|
| `Authority` | URL Keycloak realm — digunakan untuk fetch OIDC discovery |
| `ClientId` | Client ID yang harus ada di klaim `aud` token |
| `AllowedClientId` | Client ID yang harus ada di klaim `azp` (authorized party) |
| `RequireHttpsMetadata` | `false` untuk dev, `true` untuk production |

### Token Refresh Keycloak

```javascript
// axios.js interceptor
if (keycloak.authenticated) {
  await keycloak.updateToken(30)  // Refresh jika sisa < 30 detik
  config.headers.Authorization = `Bearer ${keycloak.token}`
}
```

---

## Jalur 3: DWI Mobile Bridge

### Kapan Digunakan

Saat user mengakses LMS dari dalam aplikasi DWI Mobile (deep-link), bukan via browser langsung.

### Alur Token Exchange

```
1. DWI Mobile buka LMS URL dengan:
   https://lms.example.com?_lms_auth=CODE
         │
         ▼
2. Frontend (App.vue / router) deteksi param _lms_auth
         │
         ▼
3. Frontend POST ke backend:
   /api/auth/lms-exchange?code=CODE
         │
         ▼
4. Backend validasi code ke DWI Mobile server
   Return: { access_token, refresh_token, expires_in }
         │
         ▼
5. tokenStore.js menyimpan:
   - access_token: memory (volatile)
   - refresh_token: sessionStorage
         │
         ▼
6. Setiap request: inject Bearer token dari memory
         │
         ▼
7. Jika token expired:
   POST {DWI_MOBILE_URL}/api/auth/lms-refresh
   Return: access_token baru
         │
         ▼
8. Jika refresh gagal:
   clearTokens() → window.location.reload()
```

### Token Storage Strategy

```javascript
// tokenStore.js
const _tokens = {
  accessToken: null,   // Memory — hilang saat tab ditutup
  refreshToken: null,  // Dimuat dari sessionStorage
  expiresAt: null,
}

// Restore dari sessionStorage saat load
const stored = sessionStorage.getItem('lms_refresh_token')
if (stored) _tokens.refreshToken = stored
```

**Kenapa storage-nya begini?**
- **Memory** untuk access token: Jika browser ditutup, token tidak tersimpan (lebih aman dari XSS)
- **sessionStorage** untuk refresh token: Bertahan selama tab terbuka, tapi hilang jika tab ditutup (session-scoped)
- **Bukan localStorage**: Tidak persistent antar session — lebih aman

### Prioritas Interceptor Axios

```javascript
if (VITE_MOCK_AUTH === 'true')    → pakai mock headers
else if hasBridgeSession()         → pakai bridge token
else if keycloak.authenticated     → pakai Keycloak token
else                               → reject (not authenticated)
```

---

## Sistem Role

### Role yang Tersedia

| Role | Hak Akses |
|------|-----------|
| `student` | Daftar kursus, ikuti quiz, submit tugas, forum |
| `teacher` | Semua student + buat kursus, nilai mahasiswa, buat ujian |
| `admin` | Semua teacher + manajemen user, pengaturan sistem |

### Sumber Kebenaran Role

Role dikelola di **tabel `AppUser.Role`** di database LMS — **bukan** dari Keycloak.

Alasannya:
1. Admin LMS bisa mengubah role tanpa perlu akses Keycloak admin console
2. Role bisa diubah langsung dari UI LMS
3. Keycloak hanya sebagai identity provider (siapa kamu), bukan authorization provider (apa yang boleh kamu lakukan)

### Sinkronisasi User

Setiap kali user login, frontend memanggil `POST /api/auth/sync`:
- Jika user belum ada di DB → buat AppUser baru dengan role default `student`
- Jika sudah ada → update nama dan email dari token Keycloak
- Role **tidak** diubah pada sync (hanya admin yang bisa ubah role)

### Pengecekan Role di Backend

**Controller-level** (middleware):
```csharp
[Authorize(Roles = "teacher,admin")]
public async Task<IActionResult> CreateCourse(...) { ... }
```

**Resource-level** (dalam action):
```csharp
// Cek apakah teacher ini yang buat kursus ini
bool canManage = (role == "teacher" || role == "admin")
                 && course.InstructorId == currentUserId;

if (!canManage) return Forbid();
```

### Pengecekan Role di Frontend

**Route guard** (`router/index.js`):
```javascript
router.beforeEach((to, _from, next) => {
  if (to.meta.role === 'teacher' && !auth.isTeacher) {
    return next({ name: 'Dashboard' })
  }
  if (to.meta.role === 'admin' && !auth.isAdmin) {
    return next({ name: 'Dashboard' })
  }
  next()
})
```

**Computed dari Pinia store**:
```javascript
const isAdmin = computed(() => user.value?.role === 'admin')
const isTeacher = computed(() => ['teacher', 'admin'].includes(user.value?.role))
const isStudent = computed(() => user.value?.role === 'student')
```

---

## Mandatory Exam Token (Ujian Wajib)

Ujian wajib menggunakan sistem token JWT tersendiri (bukan Keycloak token) untuk deep-link.

### Generate Token

```
POST /api/mandatory-exams/{id}/generate-link
Authorization: Bearer {admin_token}
    │
    ▼
Backend:
  1. Buat JWT (HS256) dengan klaim:
     - sub: userId (atau null untuk token publik)
     - exam_id: id ujian
     - azp: "mandatory-exam"
     - jti: UUID unik (untuk revocation)
     - exp: waktu kadaluarsa
  2. Simpan MandatoryExamSession ke DB (audit trail)
  3. Return: { url: "{FrontendBaseUrl}/exam/start?token={jwt}" }
```

**Konfigurasi signing key** (`appsettings.json`):
```json
{
  "MandatoryExam": {
    "SigningKey": "min-32-chars-secret-key-here!!",
    "FrontendBaseUrl": "http://localhost:5173"
  }
}
```

### Validasi Token saat Ujian Mulai

```
POST /api/mandatory-exams/start
X-Exam-Token: {jwt}
    │
    ▼
Backend:
  1. Parse JWT, validasi signature dengan SigningKey
  2. Cek expiry
  3. Cek TokenJti di MandatoryExamSession → apakah IsRevoked = true
  4. Cek MaxUsageCount (untuk link token)
  5. Assign exam ke user (buat MandatoryExamAssignment jika belum ada)
  6. Update sesi: UsedAt, CurrentUsageCount
  7. Return attempt ID untuk mulai mengerjakan
```

### Jenis Token

| Jenis | Keterangan |
|-------|-----------|
| **Personal Token** | Untuk satu user spesifik (`sub` = userId) |
| **Link Token** | Untuk siapa saja (`sub` = null), bisa dipakai multiple user dengan MaxUsageCount |

---

## Keamanan Tambahan

### Validasi Klaim JWT

Backend melakukan pengecekan `azp` (Authorized Party) pada setiap token Keycloak:

```csharp
// OnTokenValidated
var azp = context.Principal.FindFirstValue("azp");
if (azp != allowedClientId)
    context.Fail($"azp '{azp}' bukan client yang diizinkan.");
```

Tujuan: Mencegah token yang diissued ke client lain (e.g. token mobile app) bisa dipakai di LMS API.

### Clock Skew

```csharp
TokenValidationParameters = new() {
    ClockSkew = TimeSpan.FromSeconds(30)
}
```

Toleransi 30 detik untuk perbedaan jam antara server LMS dan Keycloak.

### Debug Claims (Dev Only)

```
GET /api/auth/debug-claims
Authorization: Bearer {token}
```

Response: Semua claim dari token, termasuk `azp`, `sub`, `aud`, role, dll.
Hanya tersedia di Development environment.

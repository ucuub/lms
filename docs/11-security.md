# 11 — Keamanan

## Gambaran Umum

Sistem LMS mengimplementasikan beberapa lapisan keamanan:
1. Autentikasi JWT (Keycloak)
2. Validasi token mandatory exam (HS256)
3. Otorisasi berbasis role
4. CORS whitelist
5. Rate limiting
6. Validasi file upload
7. Token storage yang aman di frontend

---

## 1. Validasi JWT (Backend)

### Klaim yang Divalidasi

| Klaim | Validasi |
|-------|---------|
| Signature | Diverifikasi dengan JWKS dari Keycloak OIDC discovery |
| `iss` (Issuer) | Harus sama persis dengan `Keycloak:Authority` |
| `aud` (Audience) | Harus mengandung `Keycloak:ClientId` |
| `exp` (Expiry) | Harus belum kadaluarsa (toleransi 30 detik) |
| `azp` | Harus sama dengan `Keycloak:AllowedClientId` |
| `sub` | Harus tidak kosong |

### Kenapa Cek `azp`?

`azp` (Authorized Party) adalah client yang *meminta* token. Jika tidak dicek, token yang diissued ke aplikasi lain (misalnya mobile app untuk keperluan lain) bisa disalahgunakan di LMS API.

```csharp
// Hanya token dari "dwi-mobile" yang diterima
if (azp != "dwi-mobile")
    context.Fail("azp bukan client yang diizinkan");
```

### Issuer Pinning

Walaupun Keycloak OIDC discovery sudah otomatis validasi issuer, sistem ini juga pin secara eksplisit:

```csharp
ValidateIssuer = true,
ValidIssuer = authority,  // Defense-in-depth: pin issuer secara eksplisit
```

Ini mencegah token dari realm Keycloak lain (yang mungkin JWKS-nya bocor) diterima.

---

## 2. Role-Based Authorization

### Hirarki Role

```
admin
  └── bisa semua yang teacher bisa
teacher
  └── bisa semua yang student bisa
student
```

### Resource-Level Authorization

Selain role-level check, sistem juga melakukan resource ownership check:

```csharp
// Hanya instruktur kursus yang bisa edit
var course = await db.Courses.FindAsync(id);
if (course.InstructorId != UserId && role != "admin")
    return Forbid();
```

```csharp
// Hanya user sendiri yang bisa edit post sendiri
// (kecuali teacher/admin bisa hapus post siapapun)
if (post.UserId != UserId && role is not ("teacher" or "admin"))
    return Forbid();
```

### Role Dikelola di Database LMS

Bukan dari Keycloak — mencegah privilege escalation jika Keycloak dikompromis atau misconfigured:

```csharp
// AppUserClaimsTransformation.cs
var appUser = await db.AppUsers.FirstOrDefaultAsync(u => u.UserId == sub);
if (appUser != null)
    claims.Add(new Claim("role", appUser.Role)); // DB role, bukan Keycloak role
```

---

## 3. Token Mandatory Exam

### Mengapa Token Terpisah?

Ujian wajib bisa diakses via deep-link oleh pengguna yang mungkin tidak punya akun Keycloak. Sistem menggunakan JWT tersendiri yang di-sign oleh backend.

### Keamanan Token

- Algorithm: **HS256** (HMAC SHA-256)
- Signing key: Minimum 32 karakter, dikonfigurasi via `MandatoryExam:SigningKey`
- Claims: `sub`, `exam_id`, `azp`, `jti` (unique ID), `exp`

### Revocation

JWT tidak bisa di-revoke secara native (stateless). Sistem mengatasi ini dengan:
1. Setiap token punya `jti` (JWT ID) yang unik
2. `jti` disimpan di `MandatoryExamSession`
3. Saat validasi, cek `IsRevoked = true` di tabel tersebut
4. Jika revoked → tolak token meskipun signature valid

```csharp
var session = await db.MandatoryExamSessions
    .FirstOrDefaultAsync(s => s.TokenJti == jti);

if (session.IsRevoked)
    return Unauthorized("Token telah dicabut");
```

### Limit Penggunaan (Link Token)

Token publik (untuk siapa saja) bisa dibatasi penggunaannya:

```csharp
if (session.MaxUsageCount > 0
    && session.CurrentUsageCount >= session.MaxUsageCount)
    return Unauthorized("Token sudah mencapai batas penggunaan");
```

---

## 4. CORS (Cross-Origin Resource Sharing)

### Konfigurasi

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)  // Whitelist, bukan wildcard
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Prinsip

- **Tidak menggunakan `*`** untuk origins — hanya origin yang didaftarkan yang diizinkan
- `AllowCredentials()` memungkinkan cookie/authorization header dikirim
- Header dan method lainnya diizinkan penuh karena validasi sudah di autentikasi

---

## 5. Rate Limiting

Melindungi dari:
- Brute force
- DoS sederhana
- Abuse API

| Policy | Limit | Target |
|--------|-------|--------|
| `global` | 100 req/mnt per IP | Semua endpoint |
| `upload` | 10 req/mnt per IP | Endpoint upload file |

Response saat limit terlewati:
```json
HTTP 429 Too Many Requests
{ "message": "Terlalu banyak request. Coba lagi nanti." }
```

---

## 6. Keamanan File Upload

### Validasi di Backend

```
1. Cek ukuran file (sebelum baca penuh)
2. Cek ekstensi (whitelist)
3. Cek MIME type (whitelist)
4. Generate nama file unik (bukan pakai nama asli dari client)
5. Simpan di folder uploads (bukan di root web)
```

### Ekstensi yang Diizinkan

Bergantung pada endpoint:
- **Avatar**: `.jpg`, `.jpeg`, `.png`, `.webp` (maks 5MB)
- **Thumbnail kursus**: `.jpg`, `.jpeg`, `.png`, `.webp` (maks 5MB)
- **Attachment modul**: `.pdf`, `.docx`, `.xlsx`, `.pptx`, `.jpg`, `.png` (maks 50MB)
- **Submission tugas**: `.pdf`, `.docx`, `.xlsx`, `.zip`, `.jpg`, `.png` (maks 50MB)

### Risiko yang Diatasi

- **Path traversal**: Nama file digenerate otomatis, bukan dari input client
- **File type spoofing**: Validasi MIME type, bukan hanya ekstensi
- **Arbitrary code execution**: Ekstensi executable (`.exe`, `.bat`, `.sh`, `.php`) tidak diizinkan

---

## 7. Keamanan Token di Frontend

### Strategi Penyimpanan

| Token | Storage | Alasan |
|-------|---------|--------|
| Keycloak access token | Memory (keycloak.js adapter) | Volatile, tidak persist |
| Keycloak refresh token | Dikelola oleh keycloak.js | Internal adapter |
| DWI Bridge access token | Memory JavaScript | Hilang saat tab ditutup |
| DWI Bridge refresh token | `sessionStorage` | Bertahan per-tab, hilang saat tab ditutup |

### Kenapa Tidak `localStorage`?

`localStorage` persists antar session dan antar tab. Jika ada XSS di halaman manapun di domain yang sama, attacker bisa mencuri token dari `localStorage`.

`sessionStorage` lebih aman karena:
- Scoped ke satu tab
- Hilang saat tab ditutup
- Tidak bisa diakses dari tab lain

### Kenapa Memory untuk Access Token?

Memory JavaScript hilang saat halaman di-reload atau tab ditutup. Access token yang pendek umurnya (5 menit) tidak perlu persist — user bisa refresh menggunakan refresh token.

---

## 8. Global Error Handling

Backend menangkap semua exception di middleware:

```csharp
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        ctx.Response.StatusCode = 500;
        ctx.Response.ContentType = "application/json";
        // Pesan error generik — tidak expose stack trace ke client
        await ctx.Response.WriteAsJsonAsync(new
        {
            message = "Terjadi kesalahan internal server."
        });
    });
});
```

**Stack trace tidak pernah dikirim ke client** — ini mencegah information disclosure yang bisa membantu attacker.

---

## 9. Secrets Management

### Secrets yang Perlu Dijaga

| Secret | Lokasi | Risiko Jika Bocor |
|--------|--------|--------------------|
| `MandatoryExam:SigningKey` | appsettings | Pemalsuan token ujian |
| `ServiceIntegration:ApiKey` | appsettings | Bypass validasi DWI Mobile |
| `LLM:ApiKey` | appsettings | Penyalahgunaan API LLM (biaya) |
| Database password | appsettings | Akses penuh ke database |
| Keycloak client secret | Keycloak | Pemalsuan identitas client |

### Best Practices

1. **Jangan commit** `appsettings.Production.json` ke git (tambahkan ke `.gitignore`)
2. Gunakan **environment variables** di production:
   ```
   MandatoryExam__SigningKey=SECRET
   ```
3. Gunakan **ASP.NET Core User Secrets** untuk development:
   ```cmd
   dotnet user-secrets set "MandatoryExam:SigningKey" "dev-secret"
   ```
4. Di Kubernetes/Docker: gunakan **Secrets** atau **Vault**

---

## 10. Checklist Keamanan Production

### Backend

- [ ] `RequireHttpsMetadata: true` di Keycloak config
- [ ] `MandatoryExam:SigningKey` sudah diganti (minimum 32 karakter random)
- [ ] `ServiceIntegration:ApiKey` sudah diganti
- [ ] CORS `AllowedOrigins` hanya berisi domain production (bukan localhost)
- [ ] Database password kuat dan tidak default
- [ ] Aplikasi tidak berjalan sebagai user root/admin
- [ ] File `wwwroot/uploads` tidak bisa dieksekusi (hanya serve static)
- [ ] HTTPS aktif (RequireHttpsMetadata = true)
- [ ] Log level production tidak terlalu verbose

### Frontend

- [ ] `VITE_MOCK_AUTH` tidak ada atau `false`
- [ ] `VITE_API_URL` menggunakan HTTPS
- [ ] Build menggunakan mode production (Vite otomatis minify dan strip dev code)
- [ ] Tidak ada API key atau secret hardcoded di kode Vue

### Keycloak

- [ ] HTTPS diaktifkan di Keycloak
- [ ] Token lifetime dikonfigurasi sesuai kebutuhan (tidak terlalu panjang)
- [ ] Audience mapper dikonfigurasi dengan benar
- [ ] Admin console Keycloak tidak diekspos ke internet publik
- [ ] Password admin Keycloak sudah diganti dari default

---

## 11. Endpoint Khusus Development

Endpoint berikut hanya tersedia di environment Development dan otomatis diblok di Production:

| Endpoint | Keterangan |
|----------|-----------|
| `GET /api/auth/debug-claims` | Dump semua JWT claims |
| Mock auth via `X-Mock-*` headers | Bypass autentikasi JWT |

Pastikan `ASPNETCORE_ENVIRONMENT=Production` saat deploy agar tidak ada endpoint dev yang terekspos.

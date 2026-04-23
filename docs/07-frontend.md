# 07 — Arsitektur Frontend

## Tech Stack Frontend

| Teknologi | Versi | Peran |
|-----------|-------|-------|
| Vue 3 | 3.5.x | Framework UI (Composition API) |
| Pinia | 3.0.x | State management |
| Vue Router | 4.6.x | Client-side routing |
| Axios | 1.14.x | HTTP client |
| Tailwind CSS | 3.4.x | Utility-first styling |
| Vite | 5.4.x | Build tool & dev server |
| Keycloak.js | 26.x | Keycloak adapter (production) |

---

## Entry Point

**`src/main.js`**:
```javascript
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import App from './App.vue'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.mount('#app')
```

---

## Vue Router

### Semua Route

**Route publik**:

| Path | Komponen | Keterangan |
|------|----------|-----------|
| `/exam/start` | MandatoryExamView | Deep-link ujian wajib, tanpa auth |
| `/access-denied` | AccessDeniedView | Halaman akses ditolak |

**Route terproteksi** (butuh auth, semua dibungkus `AppLayout`):

| Path | Komponen |
|------|----------|
| `/dashboard` | DashboardView |
| `/courses` | CourseListView |
| `/courses/create` | CourseFormView (teacher) |
| `/courses/:id` | CourseDetailView |
| `/courses/:id/edit` | CourseFormView (teacher) |
| `/courses/:courseId/question-bank` | CourseQuestionBankView (teacher) |
| `/courses/:courseId/modules/create` | ModuleFormView (teacher) |
| `/courses/:courseId/modules/:moduleId` | ModuleView |
| `/courses/:courseId/modules/:moduleId/edit` | ModuleFormView (teacher) |
| `/courses/:courseId/assignments/:id` | AssignmentView |
| `/courses/:courseId/assignments/:id/submissions` | SubmissionsView (teacher) |
| `/courses/:courseId/quizzes/:id` | QuizView |
| `/courses/:courseId/quizzes/:id/take` | QuizTakeView |
| `/courses/:courseId/quizzes/:id/result/:attemptId` | QuizResultView |
| `/courses/:courseId/quizzes/:id/manage` | QuizManageView (teacher) |
| `/courses/:courseId/forum` | ForumView |
| `/courses/:courseId/forum/create` | ForumCreateView |
| `/courses/:courseId/forum/:threadId` | ForumThreadView |
| `/courses/:courseId/gradebook` | GradebookView |
| `/certificates` | CertificatesView |
| `/calendar` | CalendarView |
| `/notifications` | NotificationsView |
| `/messages` | MessagesView |
| `/activity` | ActivityView |
| `/practice` | PracticeListView |
| `/practice/:id/attempt` | PracticeAttemptView |
| `/practice/attempt/:id/result` | PracticeResultView |
| `/ujian` | QuestionSetListView |
| `/ujian/create` | QuestionSetCreateView (teacher) |
| `/ujian/:id/manage` | QuestionSetManageView (teacher) |
| `/ujian/:id/take` | QuestionSetTakeView |
| `/ujian/attempt/:id/grade` | QuestionSetGradeView (teacher) |
| `/ujian/attempt/:id/result` | QuestionSetResultView |
| `/exam/manage` | MandatoryExamManageView (teacher) |
| `/admin/dashboard` | AdminDashboardView (admin) |
| `/admin/users` | AdminUsersView (admin) |
| `/admin/courses` | AdminCoursesView (admin) |

### Route Guard

```javascript
router.beforeEach(async (to, _from, next) => {
  // Route publik (exam/start, access-denied)
  if (to.meta.public) return next()

  // Cek autentikasi
  const auth = useAuthStore()
  if (!auth.isAuthenticated) {
    // Mock auth: redirect ke access denied
    // Keycloak: tunggu keycloak.init()
  }

  // Cek role
  if (to.meta.role === 'teacher' && !auth.isTeacher)
    return next({ name: 'Dashboard' })
  if (to.meta.role === 'admin' && !auth.isAdmin)
    return next({ name: 'Dashboard' })

  next()
})
```

---

## Pinia Stores

### Auth Store (`stores/auth.js`)

```javascript
// State
const user = ref(null)

// Getters
const isAuthenticated = computed(() => !!user.value)
const isAdmin    = computed(() => user.value?.role === 'admin')
const isTeacher  = computed(() => ['teacher', 'admin'].includes(user.value?.role))
const isStudent  = computed(() => user.value?.role === 'student')

// Actions
async function fetchMe() {
  const res = await authApi.getMe()
  user.value = res.data
}

function setUser(userData) { user.value = userData }
function logout() { user.value = null }
```

**Penggunaan dalam komponen**:
```javascript
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
// auth.isTeacher → true/false
// auth.user.name → nama user
```

### Notifications Store (`stores/notifications.js`)

```javascript
// State
const notifications = ref([])
const unreadCount   = ref(0)

// Actions
async function fetchNotifications()   // GET /api/notifications
async function markAsRead(id)         // PUT /api/notifications/{id}/read
async function markAllRead()          // POST /api/notifications/mark-all-read
function deleteNotification(id)       // Remove dari state lokal
```

---

## API Layer

Semua komunikasi dengan backend dilakukan melalui modul API di `src/api/`.

### Axios Instance (`api/axios.js`)

```javascript
const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  timeout: 15000,
})
```

### Semua Modul API

| File | Endpoints |
|------|-----------|
| `auth.js` | `/auth/sync`, `/auth/me`, `/auth/profile`, `/auth/avatar` |
| `courses.js` | `/courses`, `/courses/:id`, `/courses/:id/enroll`, resources, modules |
| `quizzes.js` | `/quizzes`, questions, attempts, bank import |
| `assignments.js` | `/assignments`, submissions, grading |
| `questionSets.js` | `/ujian`, questions, attempts, grading, export |
| `exams.js` | `/exams`, questions, attempts |
| `practice.js` | `/practice`, attempts, results |
| `mandatoryExam.js` | `/mandatory-exams`, assign, link generation, sessions, export |
| `forum.js` | `/forum`, threads, replies, likes, pins |
| `messages.js` | `/messages/conversations`, messages |
| `notifications.js` | `/notifications`, mark-read |
| `dashboard.js` | `/dashboard` |
| `gradebook.js` | `/gradebook`, items, entries |
| `activity.js` | `/activity` |
| `search.js` | `/search` |
| `ai.js` | `/ai/generate-questions` |
| `courseQuestionBank.js` | `/courses/:id/question-bank` |

### Contoh Penggunaan API

```javascript
// Di dalam component/view
import { coursesApi } from '@/api/courses'

// Fetch semua kursus
const { data } = await coursesApi.getAll({ page: 1, pageSize: 20 })
// → data.items, data.totalCount, data.page

// Buat kursus baru
await coursesApi.create({
  title: 'Judul Kursus',
  description: 'Deskripsi...',
  category: 'Pemrograman',
  level: 'Pemula'
})
```

---

## Komponen

### AppLayout

Shell utama aplikasi. Berisi:
- Sidebar navigasi
- Header dengan nama user, notifikasi bell, logout
- `<router-view>` untuk content area
- Toast container global

### CourseCard

Menampilkan satu kursus dalam grid:
- Thumbnail
- Judul, instruktur, kategori, level
- Badge status (enrolled, completed)
- Link ke halaman detail

### Pagination

```vue
<Pagination
  :total="totalCount"
  :page="currentPage"
  :page-size="20"
  @change="onPageChange"
/>
```

### AlertMessage

```vue
<AlertMessage type="error" :message="errorMsg" />
<AlertMessage type="success" :message="successMsg" />
```

Tipe: `error`, `success`, `warning`, `info`

### MockUserSwitcher

Hanya muncul saat `VITE_MOCK_AUTH=true`. Memungkinkan ganti user/role tanpa logout:

```javascript
// Klik user → simpan ke localStorage
localStorage.setItem('mockUser', JSON.stringify({
  id: 'teacher1',
  role: 'teacher',
  name: 'Teacher 1'
}))
// Reload halaman
window.location.reload()
```

### AiGenerateModal

Modal untuk generasi soal otomatis via AI. Input:
- Jumlah soal
- Tipe soal (MC, esai, dll.)
- Topik / konteks
- Tingkat kesulitan

Setelah generate, soal langsung bisa disimpan ke bank soal kursus.

### ToastContainer & useToast

```javascript
// Di composable useToast.js
const { toast } = useToast()

toast.success('Berhasil disimpan!')
toast.error('Terjadi kesalahan')
toast.info('Info tambahan')
```

---

## Environment Variables

### `.env` (default / production)

```env
VITE_API_URL=http://localhost:5000/api
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=lms
VITE_KEYCLOAK_CLIENT_ID=lms-app
VITE_DWI_MOBILE_URL=http://localhost:8000
```

### `.env.development` (dev lokal)

```env
VITE_API_URL=/api
VITE_MOCK_AUTH=true
```

`VITE_API_URL=/api` → request `/api/*` diproxy oleh Vite ke `http://localhost:5000`

### `.env.local` (override lokal, tidak di-commit)

```env
VITE_MOCK_AUTH=true
VITE_API_URL=/api
```

### Prioritas Env Vite

1. `.env.local`
2. `.env.development.local` (mode dev)
3. `.env.development`
4. `.env`

---

## Vite Config

**`vite.config.js`**:

```javascript
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 5173,
    host: true,
    allowedHosts: 'all',
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true
      },
      '/uploads': {
        target: 'http://localhost:5000',
        changeOrigin: true
      }
    }
  }
})
```

Alias `@` → `src/` memungkinkan import seperti:
```javascript
import { coursesApi } from '@/api/courses'
import AppLayout from '@/components/AppLayout.vue'
```

---

## Auth di Frontend

### Keycloak Init (`auth/keycloak.js`)

```javascript
import Keycloak from 'keycloak-js'

const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL,
  realm: import.meta.env.VITE_KEYCLOAK_REALM,
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID,
})

export default keycloak
```

Keycloak diinit di `App.vue` atau `main.js` dengan mode `check-sso` (tidak redirect otomatis ke login page).

### Token Store (`auth/tokenStore.js`)

Untuk DWI Mobile Bridge:

```javascript
// Cek apakah ada sesi bridge
export function hasBridgeSession() {
  return !!(_tokens.accessToken || _tokens.refreshToken)
}

// Validasi access token masih valid
export function isAccessTokenValid() {
  return _tokens.expiresAt && Date.now() < _tokens.expiresAt - 5000
}

// Simpan token dari exchange
export function setTokens(access, refresh, expiresIn) {
  _tokens.accessToken = access
  _tokens.expiresAt = Date.now() + expiresIn * 1000
  _tokens.refreshToken = refresh
  sessionStorage.setItem('lms_refresh_token', refresh)
}

// Hapus semua token
export function clearTokens() {
  _tokens.accessToken = null
  _tokens.refreshToken = null
  _tokens.expiresAt = null
  sessionStorage.removeItem('lms_refresh_token')
}
```

---

## Struktur Views Per Fitur

### Kursus
- `CourseListView` — Browse semua kursus (card grid + filter)
- `CourseDetailView` — Detail kursus: modul, seksi, quiz, tugas, forum
- `CourseFormView` — Form create/edit kursus (teacher)
- `CourseQuestionBankView` — Kelola bank soal kursus (teacher)

### Modul
- `ModuleView` — Tampil konten modul (teks, video embed, attachment)
- `ModuleFormView` — Form create/edit modul (teacher)

### Quiz
- `QuizView` — Info quiz sebelum mulai
- `QuizTakeView` — Mengerjakan quiz (timer, soal, submit)
- `QuizResultView` — Hasil attempt
- `QuizManageView` — Kelola soal quiz (teacher)

### Ujian (Question Set)
- `QuestionSetListView` — List semua ujian
- `QuestionSetCreateView` — Form buat ujian (teacher)
- `QuestionSetManageView` — Kelola soal + lihat hasil (teacher)
- `QuestionSetTakeView` — Mengerjakan ujian (student)
- `QuestionSetGradeView` — Nilai esai manual (teacher)
- `QuestionSetResultView` — Lihat hasil ujian

### Ujian Wajib
- `MandatoryExamManageView` — Kelola semua mandatory exam, assign, generate link
- `MandatoryExamView` — Halaman ujian via deep-link (public, tanpa auth biasa)

### Forum
- `ForumView` — List thread diskusi kursus
- `ForumCreateView` — Form buat thread baru
- `ForumThreadView` — Baca thread + balasan + form reply

### Admin
- `AdminDashboardView` — Statistik sistem
- `AdminUsersView` — Kelola user + ubah role
- `AdminCoursesView` — Kelola semua kursus

---

## Composables

### `useToast`

```javascript
import { useToast } from '@/composables/useToast'

const { toast } = useToast()
toast.success('Berhasil!')
toast.error('Gagal: ' + errorMessage)
toast.warning('Perhatian!')
toast.info('Info')
```

Setiap toast otomatis hilang setelah beberapa detik.

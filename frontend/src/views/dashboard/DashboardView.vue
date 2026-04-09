<template>
  <div class="p-6 max-w-7xl mx-auto">
    <!-- Header -->
    <div class="mb-8">
      <h1 class="text-2xl font-bold text-gray-900">Dashboard</h1>
      <p class="text-gray-500 mt-1">Selamat datang kembali, <span class="font-medium text-gray-700">{{ auth.user?.name }}</span></p>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="space-y-4">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="card p-5 animate-pulse">
          <div class="h-3 bg-gray-200 rounded w-2/3 mb-3"></div>
          <div class="h-8 bg-gray-200 rounded w-1/2"></div>
        </div>
      </div>
      <div class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div v-for="i in 3" :key="i" class="card animate-pulse">
          <div class="aspect-video bg-gray-200"></div>
          <div class="p-4 space-y-2">
            <div class="h-3 bg-gray-200 rounded w-3/4"></div>
            <div class="h-3 bg-gray-200 rounded w-1/2"></div>
          </div>
        </div>
      </div>
    </div>

    <!-- Student Dashboard -->
    <template v-else-if="auth.isStudent">
      <!-- Stats -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="card p-5">
          <p class="text-sm text-gray-500">Kursus Aktif</p>
          <p class="text-3xl font-bold text-blue-600 mt-1">{{ dashboard?.stats?.activeCourses ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Selesai</p>
          <p class="text-3xl font-bold text-green-600 mt-1">{{ dashboard?.stats?.completedCourses ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Tugas Pending</p>
          <p class="text-3xl font-bold text-yellow-600 mt-1">{{ dashboard?.stats?.pendingAssignments ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Sertifikat</p>
          <p class="text-3xl font-bold text-purple-600 mt-1">{{ dashboard?.stats?.certificates ?? 0 }}</p>
        </div>
      </div>

      <!-- Upcoming Deadlines -->
      <div v-if="dashboard?.upcomingDeadlines?.length" class="card p-6 mb-6">
        <h2 class="font-semibold text-gray-900 mb-3 flex items-center gap-2">
          <svg class="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          Deadline Mendatang
        </h2>
        <div class="space-y-2">
          <RouterLink v-for="d in dashboard.upcomingDeadlines" :key="d.id + d.type"
            :to="d.link || '#'"
            class="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition">
            <div>
              <p class="text-sm font-medium text-gray-800">{{ d.title }}</p>
              <p class="text-xs text-gray-500">{{ d.courseTitle }} · {{ d.type === 'assignment' ? 'Tugas' : 'Quiz' }}</p>
            </div>
            <span :class="['text-xs font-medium px-2 py-0.5 rounded-full', isUrgent(d.dueDate) ? 'bg-red-100 text-red-700' : 'bg-yellow-100 text-yellow-700']">
              {{ formatDeadline(d.dueDate) }}
            </span>
          </RouterLink>
        </div>
      </div>

      <!-- My Courses -->
      <div class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-lg font-semibold text-gray-900">Kursus Saya</h2>
          <RouterLink to="/courses" class="text-sm text-blue-600 hover:underline">Jelajahi kursus →</RouterLink>
        </div>

        <div v-if="!dashboard?.courses?.length" class="card p-12 text-center">
          <svg class="w-12 h-12 text-gray-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
          </svg>
          <p class="text-gray-500">Belum ada kursus. <RouterLink to="/courses" class="text-blue-600 hover:underline">Mulai belajar sekarang!</RouterLink></p>
        </div>

        <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
          <RouterLink v-for="c in dashboard.courses" :key="c.courseId" :to="`/courses/${c.courseId}`"
            class="card hover:shadow-md transition-shadow block">
            <div class="aspect-video bg-gradient-to-br from-blue-100 to-blue-50 overflow-hidden">
              <img v-if="c.thumbnailUrl" :src="c.thumbnailUrl" :alt="c.title" class="w-full h-full object-cover" />
              <div v-else class="w-full h-full flex items-center justify-center">
                <svg class="w-10 h-10 text-blue-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
                </svg>
              </div>
            </div>
            <div class="p-4">
              <h3 class="font-semibold text-gray-900 line-clamp-1 mb-1">{{ c.title }}</h3>
              <div class="flex justify-between text-xs text-gray-500 mb-2">
                <span>{{ c.completedModules }}/{{ c.totalModules }} modul</span>
                <span :class="c.isCompleted ? 'text-green-600 font-medium' : ''">
                  {{ c.isCompleted ? 'Selesai' : `${c.progressPercentage}%` }}
                </span>
              </div>
              <div class="progress-bar">
                <div class="progress-fill" :class="c.isCompleted ? 'bg-green-500' : ''" :style="`width: ${c.progressPercentage}%`"></div>
              </div>
            </div>
          </RouterLink>
        </div>
      </div>
    </template>

    <!-- Teacher Dashboard -->
    <template v-else-if="auth.isTeacher && !auth.isAdmin">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="card p-5">
          <p class="text-sm text-gray-500">Kursus Saya</p>
          <p class="text-3xl font-bold text-blue-600 mt-1">{{ dashboard?.stats?.totalCourses ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Total Siswa</p>
          <p class="text-3xl font-bold text-green-600 mt-1">{{ dashboard?.stats?.totalStudents ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Quiz Aktif</p>
          <p class="text-3xl font-bold text-yellow-600 mt-1">{{ dashboard?.stats?.activeQuizzes ?? 0 }}</p>
        </div>
        <div class="card p-5">
          <p class="text-sm text-gray-500">Perlu Dinilai</p>
          <p class="text-3xl font-bold text-red-600 mt-1">{{ dashboard?.stats?.ungradedSubmissions ?? 0 }}</p>
        </div>
      </div>

      <!-- Pending Grading -->
      <div v-if="dashboard?.pendingGrading?.length" class="card p-6 mb-6">
        <h2 class="font-semibold text-gray-900 mb-3">Tugas Menunggu Penilaian</h2>
        <div class="space-y-2">
          <RouterLink v-for="p in dashboard.pendingGrading.slice(0, 5)" :key="p.submissionId"
            :to="`/courses/${p.courseId}/assignments/${p.assignmentId}/submissions`"
            class="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition">
            <div>
              <p class="text-sm font-medium text-gray-800">{{ p.studentName }}</p>
              <p class="text-xs text-gray-500">{{ p.courseTitle }} · {{ p.assignmentTitle }}</p>
            </div>
            <span class="text-xs text-gray-400">{{ new Date(p.submittedAt).toLocaleDateString('id-ID') }}</span>
          </RouterLink>
        </div>
      </div>

      <div class="flex items-center justify-between mb-4">
        <h2 class="text-lg font-semibold text-gray-900">Kursus Saya</h2>
        <RouterLink to="/courses/create" class="btn-primary btn-sm">+ Buat Kursus</RouterLink>
      </div>

      <div v-if="!dashboard?.courses?.length" class="card p-12 text-center">
        <p class="text-gray-500">Belum ada kursus. <RouterLink to="/courses/create" class="text-blue-600 hover:underline">Buat sekarang!</RouterLink></p>
      </div>
      <div v-else class="grid sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <RouterLink v-for="c in dashboard.courses" :key="c.courseId" :to="`/courses/${c.courseId}`"
          class="card hover:shadow-md transition-shadow block p-4">
          <div class="flex items-start justify-between mb-2">
            <h3 class="font-semibold text-gray-900 line-clamp-2 flex-1">{{ c.title }}</h3>
            <span v-if="!c.isPublished" class="badge-yellow ml-2 shrink-0">Draft</span>
          </div>
          <div class="flex gap-4 text-sm text-gray-500">
            <span>{{ c.enrollmentCount }} siswa</span>
            <span>{{ c.moduleCount }} modul</span>
            <span v-if="c.averageRating > 0">⭐ {{ c.averageRating.toFixed(1) }}</span>
          </div>
        </RouterLink>
      </div>
    </template>

    <!-- Admin Dashboard — redirect ke halaman admin yang lebih lengkap -->
    <template v-else-if="auth.isAdmin">
      <div class="card p-12 text-center">
        <svg class="w-16 h-16 text-blue-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
        </svg>
        <h2 class="text-lg font-semibold text-gray-900 mb-2">Panel Admin</h2>
        <p class="text-gray-500 mb-6">Gunakan dashboard admin untuk statistik lengkap dan manajemen sistem.</p>
        <div class="flex justify-center gap-3">
          <RouterLink to="/admin" class="btn-primary">Dashboard Admin</RouterLink>
          <RouterLink to="/admin/users" class="btn-outline">Kelola Pengguna</RouterLink>
          <RouterLink to="/admin/courses" class="btn-outline">Kelola Kursus</RouterLink>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { dashboardApi } from '@/api/dashboard'

const auth = useAuthStore()
const loading = ref(true)
const dashboard = ref(null)

onMounted(async () => {
  try {
    const { data } = await dashboardApi.get()
    dashboard.value = data
  } catch (e) {
    console.error('Dashboard load failed', e)
  } finally {
    loading.value = false
  }
})

function isUrgent(dateStr) {
  const diff = new Date(dateStr) - new Date()
  return diff > 0 && diff < 24 * 60 * 60 * 1000
}

function formatDeadline(dateStr) {
  const diff = new Date(dateStr) - new Date()
  if (diff <= 0) return 'Lewat'
  const h = Math.floor(diff / (1000 * 60 * 60))
  if (h < 24) return `${h}j lagi`
  const d = Math.floor(h / 24)
  return `${d}h lagi`
}
</script>

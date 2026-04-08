<template>
  <div v-if="course" class="max-w-5xl mx-auto p-6">
    <!-- Hero -->
    <div class="card overflow-hidden mb-6">
      <div class="aspect-[3/1] bg-gradient-to-r from-blue-600 to-indigo-600 relative overflow-hidden">
        <img v-if="course.thumbnailUrl" :src="course.thumbnailUrl" class="w-full h-full object-cover opacity-30" />
        <div class="absolute inset-0 flex flex-col justify-end p-8 text-white">
          <div class="flex gap-2 mb-3">
            <span v-if="course.category" class="badge bg-white/20 text-white">{{ course.category }}</span>
            <span class="badge bg-white/20 text-white">{{ course.level }}</span>
          </div>
          <h1 class="text-3xl font-bold">{{ course.title }}</h1>
          <p class="mt-2 opacity-80">{{ course.instructorName }}</p>
          <div class="flex gap-6 mt-4 text-sm opacity-80">
            <span>👥 {{ course.enrollmentCount }} siswa</span>
            <span>⭐ {{ course.averageRating > 0 ? course.averageRating.toFixed(1) : '-' }} ({{ course.reviewCount }} ulasan)</span>
            <span>📚 {{ course.modules?.length ?? 0 }} modul</span>
          </div>
        </div>
      </div>
    </div>

    <div class="grid lg:grid-cols-3 gap-6">
      <!-- Main -->
      <div class="lg:col-span-2 space-y-6">
        <!-- Description -->
        <div class="card p-6">
          <h2 class="font-semibold text-gray-900 mb-3">Tentang Kursus</h2>
          <p class="text-gray-600 whitespace-pre-line">{{ course.description }}</p>
        </div>

        <!-- Modules -->
        <div class="card p-6">
          <div class="flex items-center justify-between mb-4">
            <h2 class="font-semibold text-gray-900">Materi Kursus</h2>
            <RouterLink v-if="auth.isTeacher && isMyOwnCourse" :to="`/courses/${course.id}/modules/create`" class="btn-outline btn-sm">+ Modul</RouterLink>
          </div>

          <div v-if="course.modules?.length === 0" class="text-center py-8 text-gray-400">
            Belum ada modul.
          </div>

          <div class="space-y-2">
            <div v-for="module in course.modules" :key="module.id"
              class="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition">
              <div class="flex items-center gap-3">
                <div class="w-7 h-7 rounded-full bg-blue-100 text-blue-600 text-xs flex items-center justify-center font-medium">
                  {{ module.order }}
                </div>
                <div>
                  <p class="text-sm font-medium text-gray-800">{{ module.title }}</p>
                  <p class="text-xs text-gray-500">{{ module.durationMinutes }}m · {{ module.contentType }}</p>
                </div>
              </div>
              <RouterLink v-if="course.isEnrolled || auth.isTeacher"
                :to="`/courses/${course.id}/modules/${module.id}`"
                class="text-blue-600 text-sm hover:underline">
                Lihat
              </RouterLink>
              <svg v-else class="w-4 h-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
              </svg>
            </div>
          </div>
        </div>

        <!-- Assignments -->
        <div v-if="course.assignments?.length" class="card p-6">
          <h2 class="font-semibold text-gray-900 mb-4">Tugas</h2>
          <div class="space-y-2">
            <RouterLink v-for="a in course.assignments" :key="a.id"
              :to="`/courses/${course.id}/assignments/${a.id}`"
              class="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition">
              <div>
                <p class="text-sm font-medium text-gray-800">{{ a.title }}</p>
                <p class="text-xs text-gray-500">Deadline: {{ a.dueDate ? new Date(a.dueDate).toLocaleDateString('id-ID') : 'Tidak ada' }}</p>
              </div>
              <span class="text-xs text-gray-500">Max: {{ a.maxScore }}</span>
            </RouterLink>
          </div>
        </div>

        <!-- Quizzes -->
        <div v-if="course.quizzes?.length" class="card p-6">
          <h2 class="font-semibold text-gray-900 mb-4">Quiz</h2>
          <div class="space-y-2">
            <RouterLink v-for="q in course.quizzes" :key="q.id"
              :to="`/courses/${course.id}/quizzes/${q.id}`"
              class="flex items-center justify-between p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition">
              <div>
                <p class="text-sm font-medium text-gray-800">{{ q.title }}</p>
                <p class="text-xs text-gray-500">{{ q.questionCount }} soal · {{ q.timeLimitMinutes }} menit</p>
              </div>
              <span :class="q.isPublished ? 'badge-green' : 'badge-yellow'">
                {{ q.isPublished ? 'Aktif' : 'Draft' }}
              </span>
            </RouterLink>
          </div>
        </div>

        <!-- Reviews -->
        <div class="card p-6">
          <h2 class="font-semibold text-gray-900 mb-4">Ulasan Siswa</h2>

          <!-- Submit review -->
          <div v-if="course.isEnrolled && !auth.isTeacher" class="mb-6 p-4 bg-gray-50 rounded-lg">
            <p class="text-sm font-medium mb-2">Berikan ulasan Anda</p>
            <div class="flex gap-1 mb-3">
              <button v-for="star in 5" :key="star"
                @click="review.rating = star"
                :class="['text-2xl transition', star <= review.rating ? 'text-yellow-400' : 'text-gray-300']">
                ★
              </button>
            </div>
            <textarea v-model="review.comment" class="textarea" placeholder="Tulis komentar (opsional)..." rows="3"></textarea>
            <button @click="submitReview" :disabled="reviewLoading" class="btn-primary btn-sm mt-2">Kirim Ulasan</button>
          </div>

          <div v-if="course.reviews?.length === 0" class="text-center py-6 text-gray-400">Belum ada ulasan.</div>
          <div v-else class="space-y-4">
            <div v-for="r in course.reviews" :key="r.id" class="border-b border-gray-100 pb-4 last:border-0">
              <div class="flex items-center justify-between mb-1">
                <span class="font-medium text-sm text-gray-800">{{ r.userName }}</span>
                <div class="flex text-yellow-400 text-sm">
                  <span v-for="s in 5" :key="s">{{ s <= r.rating ? '★' : '☆' }}</span>
                </div>
              </div>
              <p class="text-sm text-gray-600">{{ r.comment }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Sidebar -->
      <div class="space-y-4">
        <div class="card p-5 sticky top-6">
          <div v-if="!course.isEnrolled && !auth.isTeacher">
            <button @click="enroll" :disabled="enrollLoading" class="btn-primary w-full text-base py-3">
              {{ enrollLoading ? 'Mendaftarkan...' : 'Daftar Kursus' }}
            </button>
            <p class="text-xs text-center text-gray-500 mt-2">Gratis · Akses seumur hidup</p>
          </div>

          <div v-else-if="course.isEnrolled" class="space-y-3">
            <div class="alert-success text-center">✓ Sudah terdaftar</div>
            <RouterLink :to="`/courses/${course.id}/gradebook`" class="btn-outline w-full">📊 Lihat Nilai</RouterLink>
            <RouterLink :to="`/courses/${course.id}/forum`" class="btn-outline w-full">💬 Forum Diskusi</RouterLink>
            <RouterLink :to="`/courses/${course.id}/certificate`" class="btn-outline w-full">🎓 Sertifikat</RouterLink>
          </div>

          <div v-if="isMyOwnCourse" class="space-y-2 mt-3">
            <RouterLink :to="`/courses/${course.id}/edit`" class="btn-outline w-full">Edit Kursus</RouterLink>
            <RouterLink :to="`/courses/${course.id}/gradebook`" class="btn-outline w-full">Gradebook</RouterLink>
            <RouterLink :to="`/courses/${course.id}/forum`" class="btn-outline w-full">Forum</RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else-if="loading" class="flex justify-center items-center h-64">
    <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { coursesApi } from '@/api/courses'

const route = useRoute()
const auth = useAuthStore()
const course = ref(null)
const loading = ref(true)
const enrollLoading = ref(false)
const reviewLoading = ref(false)
const review = ref({ rating: 5, comment: '' })

const isMyOwnCourse = computed(() =>
  auth.isTeacher && course.value?.instructorId === auth.user?.userId
)

async function load() {
  try {
    const { data } = await coursesApi.getById(route.params.id)
    course.value = data
  } finally {
    loading.value = false
  }
}

async function enroll() {
  enrollLoading.value = true
  try {
    await coursesApi.enroll(route.params.id)
    await load()
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal mendaftar.')
  } finally {
    enrollLoading.value = false
  }
}

async function submitReview() {
  if (!review.value.rating) return
  reviewLoading.value = true
  try {
    await coursesApi.submitReview(route.params.id, review.value)
    await load()
  } finally {
    reviewLoading.value = false
  }
}

onMounted(load)
</script>

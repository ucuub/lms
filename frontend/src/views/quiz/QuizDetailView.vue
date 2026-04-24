<template>
  <div class="max-w-2xl mx-auto p-6">
    <button @click="$router.back()" class="btn-outline btn-sm mb-4">← Kembali</button>
    <div v-if="quiz" class="space-y-4">
      <div class="card p-6">
        <div class="flex items-start justify-between mb-3">
          <h1 class="text-xl font-bold">{{ quiz.title }}</h1>
          <span :class="quiz.isPublished ? 'badge-green' : 'badge-yellow'" class="badge">
            {{ quiz.isPublished ? 'Aktif' : 'Draft' }}
          </span>
        </div>
        <p v-if="quiz.description" class="text-gray-600 mb-4">{{ quiz.description }}</p>
        <div class="grid grid-cols-2 gap-4 text-sm text-gray-600">
          <div>⏱ Batas Waktu: <strong>{{ quiz.timeLimitMinutes }} menit</strong></div>
          <div>🎯 Nilai Lulus: <strong>{{ quiz.passScore }}%</strong></div>
          <div>🔄 Maks Percobaan: <strong>{{ quiz.maxAttempts }}</strong></div>
          <div>❓ Jumlah Soal: <strong>{{ quiz.questionCount }}</strong></div>
        </div>
      </div>

      <!-- Tombol untuk student -->
      <div v-if="!auth.isTeacher" class="card p-6 text-center">
        <!-- Ada sesi yang belum selesai -->
        <div v-if="hasInProgress" class="space-y-3">
          <div class="p-3 rounded-lg bg-yellow-50 border border-yellow-200 text-sm text-yellow-800">
            Kamu memiliki sesi ujian yang belum diselesaikan. Lanjutkan dari soal yang sudah dijawab.
          </div>
          <RouterLink :to="`/courses/${route.params.courseId}/quizzes/${quiz.id}/take`" class="btn-primary px-8 py-3 text-base inline-block">
            Lanjutkan Quiz
          </RouterLink>
        </div>

        <!-- Mulai baru -->
        <div v-else>
          <RouterLink :to="`/courses/${route.params.courseId}/quizzes/${quiz.id}/take`" class="btn-primary px-8 py-3 text-base">
            Mulai Quiz
          </RouterLink>
          <p class="text-xs text-gray-400 mt-2">Pastikan Anda siap sebelum memulai</p>
        </div>
      </div>

      <div v-if="auth.isTeacher" class="card p-4 flex gap-2">
        <RouterLink :to="`/courses/${route.params.courseId}/quizzes/${quiz.id}/manage`" class="btn-outline">
          Kelola Soal
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { quizzesApi } from '@/api/quizzes'

const route = useRoute()
const auth = useAuthStore()
const quiz = ref(null)
const hasInProgress = ref(false)

onMounted(async () => {
  const { data } = await quizzesApi.getById(route.params.id)
  quiz.value = data

  if (!auth.isTeacher) {
    try {
      const { data: inProgress } = await quizzesApi.checkInProgress(route.params.id)
      hasInProgress.value = inProgress.hasInProgress
    } catch {
      hasInProgress.value = false
    }
  }
})
</script>

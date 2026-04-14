<template>
  <div class="max-w-4xl mx-auto p-6">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Ujian</h1>
        <p class="text-sm text-gray-500 mt-1">Kerjakan ujian yang tersedia</p>
      </div>
      <RouterLink v-if="auth.isAdmin" to="/exams/create" class="btn-primary">+ Buat Ujian</RouterLink>
    </div>

    <div v-if="loading" class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <div v-else-if="exams.length === 0" class="card p-16 text-center text-gray-400">
      Belum ada ujian yang tersedia.
    </div>

    <div v-else class="space-y-4">
      <div v-for="exam in exams" :key="exam.id" class="card p-5 hover:shadow-md transition">
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1 flex-wrap">
              <h2 class="text-base font-semibold text-gray-900">{{ exam.title }}</h2>
              <span v-if="!exam.isPublished" class="badge-yellow badge text-xs">Draft</span>
            </div>
            <p v-if="exam.description" class="text-sm text-gray-500 mb-3">{{ exam.description }}</p>
            <div class="flex flex-wrap gap-4 text-xs text-gray-500">
              <span>⏱ {{ exam.timeLimitMinutes }} menit</span>
              <span>📝 {{ exam.questionCount }} soal</span>
              <span>🎯 {{ exam.totalPoints }} poin</span>
              <span>✅ Lulus: {{ exam.passScore }}%</span>
              <span>🔁 Maks {{ exam.maxAttempts }}x percobaan</span>
            </div>
          </div>

          <div class="flex flex-col gap-2 shrink-0">
            <RouterLink v-if="auth.isAdmin" :to="`/exams/${exam.id}/manage`" class="btn-outline btn-sm text-center">
              Kelola
            </RouterLink>
            <RouterLink v-if="auth.isAdmin" :to="`/exams/${exam.id}/grading`" class="btn-outline btn-sm text-center">
              Nilai ({{ exam.attemptCount }})
            </RouterLink>
            <RouterLink v-if="exam.isPublished" :to="`/exams/${exam.id}`" class="btn-primary btn-sm text-center">
              Kerjakan
            </RouterLink>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { examsApi } from '@/api/exams'

const auth = useAuthStore()
const exams = ref([])
const loading = ref(true)

onMounted(async () => {
  try {
    const { data } = await examsApi.getAll()
    exams.value = data
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="max-w-3xl mx-auto p-6">
    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-8 w-8 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="card p-8 text-center">
      <p class="text-red-600 mb-4">{{ error }}</p>
      <RouterLink to="/practice" class="btn-outline btn-sm">Kembali</RouterLink>
    </div>

    <!-- Result -->
    <template v-else-if="result">
      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <RouterLink to="/practice" class="btn-outline btn-sm">← Kembali</RouterLink>
        <h1 class="text-xl font-bold text-gray-900 flex-1">Hasil Practice Quiz</h1>
      </div>

      <!-- Score Card -->
      <div class="card p-6 mb-6 text-center">
        <p class="text-sm text-gray-500 mb-2">{{ result.quizTitle }}</p>

        <!-- Score Circle -->
        <div class="relative w-32 h-32 mx-auto mb-4">
          <svg class="w-32 h-32 -rotate-90" viewBox="0 0 120 120">
            <circle cx="60" cy="60" r="50" fill="none" stroke="#e5e7eb" stroke-width="10"/>
            <circle cx="60" cy="60" r="50" fill="none"
              :stroke="scoreColor" stroke-width="10"
              stroke-linecap="round"
              :stroke-dasharray="`${result.score / 100 * 314.16} 314.16`"
              class="transition-all duration-1000"/>
          </svg>
          <div class="absolute inset-0 flex flex-col items-center justify-center">
            <span class="text-3xl font-bold" :class="scoreTextColor">
              {{ Math.round(result.score) }}%
            </span>
          </div>
        </div>

        <div class="flex justify-center gap-8 text-sm">
          <div class="text-center">
            <p class="text-2xl font-bold text-green-600">{{ result.correctAnswers }}</p>
            <p class="text-gray-500">Benar</p>
          </div>
          <div class="text-center">
            <p class="text-2xl font-bold text-red-500">{{ result.totalQuestions - result.correctAnswers }}</p>
            <p class="text-gray-500">Salah</p>
          </div>
          <div class="text-center">
            <p class="text-2xl font-bold text-gray-700">{{ result.totalQuestions }}</p>
            <p class="text-gray-500">Total</p>
          </div>
        </div>

        <div class="mt-4 pt-4 border-t border-gray-100 text-xs text-gray-400 flex justify-center gap-4">
          <span>Mulai: {{ formatDateTime(result.startedAt) }}</span>
          <span>Selesai: {{ formatDateTime(result.submittedAt) }}</span>
          <span>Durasi: {{ duration }}</span>
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="flex gap-3 mb-6">
        <button @click="tryAgain" :disabled="restarting" class="btn-primary flex-1">
          {{ restarting ? 'Menyiapkan...' : 'Coba Lagi' }}
        </button>
        <RouterLink to="/practice" class="btn-outline flex-1 text-center">
          Semua Practice Quiz
        </RouterLink>
      </div>

      <!-- Detail Per Soal -->
      <div class="space-y-3">
        <h2 class="font-semibold text-gray-900 text-lg">Review Jawaban</h2>

        <div v-for="(d, idx) in result.details" :key="d.questionId"
          :class="['card p-4 border-l-4', d.isCorrect ? 'border-green-500' : 'border-red-400']">
          <div class="flex items-start gap-3">
            <!-- Status Icon -->
            <div :class="[
              'w-7 h-7 rounded-full flex items-center justify-center shrink-0 text-white text-sm font-bold mt-0.5',
              d.isCorrect ? 'bg-green-500' : 'bg-red-400'
            ]">
              {{ d.isCorrect ? '✓' : '✗' }}
            </div>

            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-2">
                <span class="text-xs font-medium text-gray-400">Soal {{ idx + 1 }}</span>
                <span class="badge badge-gray badge text-xs">{{ d.type }}</span>
              </div>
              <p class="text-sm text-gray-900 font-medium mb-3">{{ d.questionText }}</p>

              <div v-if="d.type !== 'Essay'" class="space-y-1.5 text-sm">
                <!-- Jawaban user -->
                <div v-if="d.selectedOptionId" :class="[
                  'flex items-center gap-2 px-3 py-2 rounded-lg',
                  d.isCorrect ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'
                ]">
                  <span class="font-medium">{{ d.isCorrect ? '✓' : '✗' }}</span>
                  <span>{{ d.selectedOptionText }}</span>
                  <span class="text-xs opacity-70">(Jawaban kamu)</span>
                </div>
                <div v-else class="flex items-center gap-2 px-3 py-2 rounded-lg bg-gray-50 text-gray-500">
                  <span>Tidak dijawab</span>
                </div>

                <!-- Jawaban benar (jika salah) -->
                <div v-if="!d.isCorrect && d.correctOptionId"
                  class="flex items-center gap-2 px-3 py-2 rounded-lg bg-green-50 text-green-800">
                  <span class="font-medium">✓</span>
                  <span>{{ d.correctOptionText }}</span>
                  <span class="text-xs opacity-70">(Jawaban benar)</span>
                </div>
              </div>

              <div v-else class="text-sm text-gray-500 italic">
                Essay — dinilai manual oleh instruktur
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { practiceApi } from '@/api/practice'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const error = ref('')
const result = ref(null)
const restarting = ref(false)

const scoreColor = computed(() => {
  const s = result.value?.score ?? 0
  if (s >= 80) return '#22c55e'
  if (s >= 60) return '#f59e0b'
  return '#ef4444'
})

const scoreTextColor = computed(() => {
  const s = result.value?.score ?? 0
  if (s >= 80) return 'text-green-600'
  if (s >= 60) return 'text-yellow-600'
  return 'text-red-500'
})

const duration = computed(() => {
  if (!result.value) return ''
  const ms = new Date(result.value.submittedAt) - new Date(result.value.startedAt)
  const min = Math.floor(ms / 60000)
  const sec = Math.floor((ms % 60000) / 1000)
  return min > 0 ? `${min}m ${sec}s` : `${sec}s`
})

function formatDateTime(dt) {
  return new Date(dt).toLocaleString('id-ID', {
    day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit'
  })
}

async function tryAgain() {
  if (!result.value) return
  restarting.value = true
  try {
    const { data } = await practiceApi.start(result.value.practiceQuizId)
    sessionStorage.setItem(`practice_session_${data.attemptId}`, JSON.stringify(data))
    router.push(`/practice/attempt/${data.attemptId}`)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal memulai ulang.')
  } finally {
    restarting.value = false
  }
}

onMounted(async () => {
  const attemptId = route.params.attemptId
  try {
    const { data } = await practiceApi.getResult(attemptId)
    result.value = data
  } catch (e) {
    error.value = e.response?.data?.message || 'Gagal memuat hasil.'
  } finally {
    loading.value = false
  }
})
</script>

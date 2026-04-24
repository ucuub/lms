<template>
  <div class="max-w-3xl mx-auto p-6">
    <div v-if="result">
      <!-- Score Card -->
      <div :class="['card p-8 text-center mb-6', result.passed ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200']">
        <div :class="['text-6xl font-bold mb-2', result.passed ? 'text-green-600' : 'text-red-600']">
          {{ result.percentage }}%
        </div>
        <div :class="['text-2xl font-semibold mb-1', result.passed ? 'text-green-800' : 'text-red-800']">
          {{ result.passed ? 'Lulus!' : 'Belum Lulus' }}
        </div>
        <p class="text-gray-600">Skor: {{ result.score }} / {{ result.maxScore }} | Nilai lulus: {{ result.passScore }}%</p>
        <p class="text-sm text-gray-500 mt-2">{{ result.quizTitle }}</p>
      </div>

      <!-- Jawaban disembunyikan -->
      <div v-if="!result.showAnswers" class="card p-6 text-center text-gray-500">
        <svg class="w-10 h-10 mx-auto mb-3 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"/>
        </svg>
        <p class="font-medium text-gray-700 mb-1">Detail jawaban tidak ditampilkan</p>
        <p class="text-sm">Instruktur mengatur agar kunci jawaban tidak ditampilkan setelah ujian.</p>
      </div>

      <!-- Detail Jawaban (hanya jika showAnswers = true) -->
      <div v-else class="card p-6">
        <h2 class="font-semibold text-gray-900 mb-4">Detail Jawaban</h2>
        <div class="space-y-5">
          <div v-for="(a, idx) in result.answers" :key="a.questionId"
            :class="['p-4 rounded-lg border', a.needsGrading ? 'border-yellow-200 bg-yellow-50' : a.isCorrect ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50']">
            <div class="flex items-start gap-2 mb-2">
              <span class="text-lg shrink-0">
                {{ a.needsGrading ? '⏳' : a.isCorrect ? '✅' : '❌' }}
              </span>
              <div class="flex-1">
                <p class="font-medium text-sm text-gray-900">{{ idx + 1 }}. {{ a.questionText }}</p>
                <p class="text-xs text-gray-500 mt-0.5">{{ a.points }} poin</p>
              </div>
              <span class="text-sm font-medium text-gray-700">
                {{ a.earnedPoints ?? '?' }} / {{ a.points }}
              </span>
            </div>

            <div v-if="a.type !== 'Essay'" class="ml-7 space-y-1 text-sm">
              <p v-if="a.selectedAnswer" class="text-gray-700">
                Jawaban Anda: <strong>{{ a.selectedAnswer }}</strong>
              </p>
              <p v-if="!a.isCorrect && a.correctAnswer" class="text-green-700">
                Jawaban benar: <strong>{{ a.correctAnswer }}</strong>
              </p>
            </div>

            <div v-else class="ml-7 text-sm">
              <p class="text-gray-600">Jawaban essay: {{ a.essayAnswer || '(tidak dijawab)' }}</p>
              <p v-if="a.needsGrading" class="text-yellow-700 mt-1">Menunggu penilaian dari instruktur.</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Actions -->
      <div class="flex gap-3 mt-6">
        <button @click="$router.back()" class="btn-outline">← Kembali</button>
      </div>
    </div>

    <div v-else class="flex justify-center items-center h-64">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { quizzesApi } from '@/api/quizzes'

const route = useRoute()
const result = ref(null)

onMounted(async () => {
  const { data } = await quizzesApi.getResult(route.params.attemptId)
  result.value = data
})
</script>

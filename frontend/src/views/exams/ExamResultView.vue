<template>
  <div class="max-w-3xl mx-auto p-6">
    <div v-if="loading" class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <div v-else-if="result">
      <!-- Header Hasil -->
      <div class="card p-8 text-center mb-6">
        <div :class="['w-20 h-20 rounded-full flex items-center justify-center text-3xl mx-auto mb-4',
          result.isPassed ? 'bg-green-100' : 'bg-red-100']">
          {{ result.isPassed ? '🎉' : '😞' }}
        </div>
        <h1 class="text-2xl font-bold text-gray-900 mb-1">{{ result.examTitle }}</h1>
        <p :class="['text-lg font-semibold mb-4', result.isPassed ? 'text-green-600' : 'text-red-600']">
          {{ result.isPassed ? 'Lulus!' : 'Belum Lulus' }}
        </p>
        <div class="flex justify-center gap-8 text-sm">
          <div>
            <p class="text-3xl font-bold text-gray-900">{{ result.percentage }}%</p>
            <p class="text-gray-500">Nilai</p>
          </div>
          <div>
            <p class="text-3xl font-bold text-gray-900">{{ result.score }}/{{ result.maxScore }}</p>
            <p class="text-gray-500">Poin</p>
          </div>
          <div>
            <p class="text-3xl font-bold text-gray-900">{{ result.passScore }}%</p>
            <p class="text-gray-500">Batas Lulus</p>
          </div>
        </div>
      </div>

      <!-- Pesan essay belum dinilai -->
      <div v-if="hasUngradedEssay" class="alert-warning mb-4">
        Soal essay kamu belum dinilai oleh admin. Nilai akhir mungkin akan berubah setelah penilaian selesai.
      </div>

      <!-- Detail Jawaban -->
      <div class="card p-5">
        <h2 class="font-semibold text-gray-900 mb-4">Detail Jawaban</h2>
        <div class="space-y-4">
          <div v-for="(ans, idx) in result.answers" :key="ans.questionId"
            :class="['p-4 rounded-lg border', answerClass(ans)]">
            <div class="flex items-start gap-3">
              <span class="w-6 h-6 rounded-full text-xs font-bold flex items-center justify-center shrink-0 mt-0.5"
                :class="answerBadgeClass(ans)">
                {{ idx + 1 }}
              </span>
              <div class="flex-1">
                <div class="flex items-center gap-2 mb-1">
                  <span class="text-xs text-gray-500">{{ typeLabel(ans.questionType) }}</span>
                  <span class="text-xs font-medium" :class="answerPointClass(ans)">
                    {{ ans.needsGrading ? 'Menunggu penilaian' : `${ans.earnedPoints ?? 0}/${ans.points} poin` }}
                  </span>
                </div>
                <p class="text-sm font-medium text-gray-800 mb-2">{{ ans.questionText }}</p>

                <!-- Jawaban pilihan ganda / benar salah -->
                <div v-if="ans.questionType !== 'Essay'" class="space-y-1 text-sm">
                  <p v-if="ans.selectedAnswer">
                    <span class="text-gray-500">Jawaban kamu: </span>
                    <span :class="ans.isCorrect ? 'text-green-700 font-medium' : 'text-red-600'">
                      {{ ans.selectedAnswer }}
                    </span>
                  </p>
                  <p v-else class="text-gray-400 italic">Tidak dijawab</p>
                  <p v-if="!ans.isCorrect && ans.correctAnswer">
                    <span class="text-gray-500">Jawaban benar: </span>
                    <span class="text-green-700 font-medium">{{ ans.correctAnswer }}</span>
                  </p>
                </div>

                <!-- Jawaban essay -->
                <div v-else class="text-sm">
                  <p class="text-gray-500 mb-1">Jawaban kamu:</p>
                  <p class="text-gray-700 whitespace-pre-line bg-white p-2 rounded border border-gray-100">
                    {{ ans.essayAnswer || '(tidak dijawab)' }}
                  </p>
                  <p v-if="ans.feedback" class="mt-2 text-blue-700">
                    <span class="font-medium">Feedback: </span>{{ ans.feedback }}
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="mt-6 flex justify-center">
        <RouterLink to="/exams" class="btn-outline">← Kembali ke Daftar Ujian</RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { examsApi } from '@/api/exams'

const route = useRoute()
const loading = ref(true)
const result = ref(null)

const hasUngradedEssay = computed(() => result.value?.answers.some(a => a.needsGrading))

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}

function answerClass(ans) {
  if (ans.needsGrading) return 'border-yellow-200 bg-yellow-50'
  if (ans.isCorrect) return 'border-green-200 bg-green-50'
  if (ans.isCorrect === false) return 'border-red-200 bg-red-50'
  return 'border-gray-200'
}

function answerBadgeClass(ans) {
  if (ans.needsGrading) return 'bg-yellow-200 text-yellow-800'
  if (ans.isCorrect) return 'bg-green-200 text-green-800'
  return 'bg-red-200 text-red-800'
}

function answerPointClass(ans) {
  if (ans.needsGrading) return 'text-yellow-600'
  if (ans.isCorrect) return 'text-green-600'
  return 'text-red-600'
}

onMounted(async () => {
  try {
    const { data } = await examsApi.getResult(route.params.attemptId)
    result.value = data
  } finally {
    loading.value = false
  }
})
</script>

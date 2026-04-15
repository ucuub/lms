<template>
  <div class="p-6 max-w-3xl mx-auto">
    <div v-if="loading" class="text-center py-20 text-gray-400">Memuat hasil...</div>

    <div v-else-if="result">
      <!-- Score card -->
      <div class="bg-white rounded-xl border border-gray-200 p-6 mb-6 text-center">
        <p class="text-sm text-gray-500 mb-1">{{ result.questionSetTitle }}</p>
        <div :class="result.isPassed ? 'text-green-500' : 'text-red-500'"
          class="text-6xl font-bold mb-2">
          {{ result.percentage }}%
        </div>
        <div :class="result.isPassed ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'"
          class="inline-block px-4 py-1 rounded-full text-sm font-semibold mb-3">
          {{ result.isPassed ? 'LULUS' : 'BELUM LULUS' }}
        </div>
        <div class="flex items-center justify-center gap-6 text-sm text-gray-500 mt-2">
          <span>Skor: <strong class="text-gray-800">{{ result.score }} / {{ result.maxScore }}</strong></span>
          <span>Nilai Lulus: <strong class="text-gray-800">{{ result.passScore }}%</strong></span>
        </div>
      </div>

      <!-- Needs grading notice -->
      <div v-if="hasUngradedEssay"
        class="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6 text-sm text-yellow-700">
        Terdapat soal essay yang belum dinilai. Skor finalmu akan diperbarui setelah dinilai oleh pengajar.
      </div>

      <!-- Answer review -->
      <h2 class="font-semibold text-gray-800 mb-3">Tinjauan Jawaban</h2>
      <div class="space-y-4">
        <div v-for="(ans, idx) in result.answers" :key="ans.answerId"
          class="bg-white rounded-xl border p-5"
          :class="ans.needsGrading ? 'border-yellow-200' :
                  ans.isCorrect ? 'border-green-200' : 'border-red-200'">
          <!-- Question -->
          <div class="flex items-start gap-3 mb-3">
            <span class="w-7 h-7 flex items-center justify-center rounded-full text-xs font-bold shrink-0"
              :class="ans.needsGrading ? 'bg-yellow-100 text-yellow-700' :
                      ans.isCorrect ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'">
              {{ idx + 1 }}
            </span>
            <p class="text-sm font-medium text-gray-800">{{ ans.questionText }}</p>
          </div>

          <!-- Essay -->
          <div v-if="ans.questionType === 'Essay'" class="ml-10">
            <p class="text-xs font-medium text-gray-500 mb-1">Jawabanmu:</p>
            <p class="text-sm text-gray-700 bg-gray-50 rounded-lg p-3">{{ ans.essayAnswer || '(tidak dijawab)' }}</p>
            <div v-if="ans.needsGrading" class="mt-2 text-xs text-yellow-600">Menunggu penilaian pengajar</div>
            <div v-else class="mt-2 flex items-center gap-3">
              <span class="text-sm font-medium text-gray-700">Skor: {{ ans.earnedPoints ?? 0 }} / {{ ans.points }}</span>
              <span v-if="ans.feedback" class="text-xs text-gray-500">— {{ ans.feedback }}</span>
            </div>
          </div>

          <!-- MC / TF -->
          <div v-else class="ml-10 space-y-1">
            <div class="flex items-center gap-2 text-sm">
              <span class="text-gray-500">Jawabanmu:</span>
              <span :class="ans.isCorrect ? 'text-green-600 font-medium' : 'text-red-500'">
                {{ ans.selectedAnswer || '(tidak dijawab)' }}
              </span>
            </div>
            <div v-if="!ans.isCorrect" class="flex items-center gap-2 text-sm">
              <span class="text-gray-500">Jawaban benar:</span>
              <span class="text-green-600 font-medium">{{ ans.correctAnswer }}</span>
            </div>
            <div class="text-xs text-gray-400 mt-1">
              Poin: {{ ans.earnedPoints ?? 0 }} / {{ ans.points }}
            </div>
          </div>
        </div>
      </div>

      <div class="mt-6 flex gap-3">
        <RouterLink to="/question-sets"
          class="px-4 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
          Kembali ke Daftar
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { questionSetsApi } from '@/api/questionSets'

const route = useRoute()
const loading = ref(true)
const result = ref(null)

const hasUngradedEssay = computed(() => result.value?.answers.some(a => a.needsGrading))

onMounted(async () => {
  try {
    const { data } = await questionSetsApi.getResult(Number(route.params.attemptId))
    result.value = data
  } finally {
    loading.value = false
  }
})
</script>

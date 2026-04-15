<template>
  <div class="p-6 max-w-5xl mx-auto">
    <div class="flex items-center gap-3 mb-6">
      <RouterLink to="/question-sets" class="text-gray-400 hover:text-gray-600">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
      </RouterLink>
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Nilai Jawaban</h1>
        <p v-if="setTitle" class="text-sm text-gray-500 mt-0.5">{{ setTitle }}</p>
      </div>
    </div>

    <div v-if="loading" class="text-center py-12 text-gray-400">Memuat data...</div>

    <div v-else-if="attempts.length === 0" class="text-center py-12 text-gray-400">
      Belum ada submission.
    </div>

    <div v-else>
      <!-- Summary table -->
      <div class="bg-white rounded-xl border border-gray-200 overflow-hidden mb-6">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="text-left px-4 py-3 font-medium text-gray-600">Siswa</th>
              <th class="text-left px-4 py-3 font-medium text-gray-600">Dikumpulkan</th>
              <th class="text-right px-4 py-3 font-medium text-gray-600">Skor</th>
              <th class="text-center px-4 py-3 font-medium text-gray-600">Status</th>
              <th class="text-center px-4 py-3 font-medium text-gray-600">Essay</th>
              <th class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100">
            <tr v-for="a in attempts" :key="a.attemptId" class="hover:bg-gray-50 transition-colors">
              <td class="px-4 py-3 font-medium text-gray-800">{{ a.userName }}</td>
              <td class="px-4 py-3 text-gray-500">{{ formatDate(a.submittedAt) }}</td>
              <td class="px-4 py-3 text-right font-mono">
                {{ a.score }} / {{ a.maxScore }}
                <span class="text-gray-400 text-xs ml-1">({{ a.percentage }}%)</span>
              </td>
              <td class="px-4 py-3 text-center">
                <span :class="a.isPassed ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'"
                  class="text-xs font-medium px-2 py-0.5 rounded-full">
                  {{ a.isPassed ? 'Lulus' : 'Tidak Lulus' }}
                </span>
              </td>
              <td class="px-4 py-3 text-center">
                <span v-if="a.hasUngradedEssay" class="text-xs text-yellow-600 font-medium">Perlu dinilai</span>
                <span v-else class="text-xs text-gray-400">Selesai</span>
              </td>
              <td class="px-4 py-3 text-right">
                <button @click="openGrading(a)" class="text-xs text-blue-600 hover:underline">Nilai</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Grading drawer -->
    <div v-if="selectedAttempt" class="fixed inset-0 z-50 flex">
      <div class="flex-1 bg-black/40" @click="selectedAttempt = null" />
      <div class="w-full max-w-xl bg-white flex flex-col overflow-hidden shadow-xl">
        <div class="flex items-center justify-between p-5 border-b">
          <div>
            <h3 class="font-semibold text-gray-900">{{ selectedAttempt.userName }}</h3>
            <p class="text-xs text-gray-400 mt-0.5">Skor: {{ selectedAttempt.score }} / {{ selectedAttempt.maxScore }}</p>
          </div>
          <button @click="selectedAttempt = null" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div v-if="loadingDetail" class="flex-1 flex items-center justify-center text-gray-400">Memuat...</div>
        <div v-else-if="attemptDetail" class="flex-1 overflow-y-auto p-5 space-y-4">
          <div v-for="(ans, idx) in attemptDetail.answers" :key="ans.answerId"
            class="p-4 border rounded-lg"
            :class="ans.needsGrading ? 'border-yellow-200 bg-yellow-50' :
                    ans.isCorrect ? 'border-green-100' : 'border-red-100'">
            <p class="text-sm font-medium text-gray-800 mb-2">{{ idx + 1 }}. {{ ans.questionText }}</p>

            <!-- Essay -->
            <div v-if="ans.questionType === 'Essay'">
              <p class="text-xs text-gray-500 mb-1">Jawaban:</p>
              <p class="text-sm text-gray-700 bg-white rounded p-2 border border-gray-200 mb-3">
                {{ ans.essayAnswer || '(tidak dijawab)' }}
              </p>
              <div class="flex items-center gap-3">
                <div>
                  <label class="text-xs font-medium text-gray-600">Poin (maks {{ ans.points }})</label>
                  <input type="number" min="0" :max="ans.points"
                    v-model.number="grades[ans.answerId].points"
                    class="w-20 ml-2 border border-gray-300 rounded px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <div class="flex-1">
                  <label class="text-xs font-medium text-gray-600">Feedback</label>
                  <input type="text" v-model="grades[ans.answerId].feedback"
                    placeholder="Komentar (opsional)"
                    class="w-full ml-2 border border-gray-300 rounded px-2 py-1 text-sm focus:outline-none focus:ring-1 focus:ring-blue-500" />
                </div>
                <button @click="saveGrade(ans)" :disabled="savingGrade === ans.answerId"
                  class="px-3 py-1.5 text-xs font-medium bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 transition-colors shrink-0">
                  Simpan
                </button>
              </div>
            </div>

            <!-- MC / TF -->
            <div v-else class="text-sm">
              <div class="flex gap-2">
                <span class="text-gray-500">Jawaban:</span>
                <span :class="ans.isCorrect ? 'text-green-600 font-medium' : 'text-red-500'">
                  {{ ans.selectedAnswer || '(tidak dijawab)' }}
                </span>
              </div>
              <div v-if="!ans.isCorrect" class="flex gap-2 mt-1">
                <span class="text-gray-500">Benar:</span>
                <span class="text-green-600 font-medium">{{ ans.correctAnswer }}</span>
              </div>
              <div class="text-xs text-gray-400 mt-1">{{ ans.earnedPoints ?? 0 }} / {{ ans.points }} poin</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { questionSetsApi } from '@/api/questionSets'

const route = useRoute()
const setId = Number(route.params.id)

const loading = ref(true)
const attempts = ref([])
const setTitle = ref('')

const selectedAttempt = ref(null)
const loadingDetail = ref(false)
const attemptDetail = ref(null)
const grades = reactive({})
const savingGrade = ref(null)

onMounted(async () => {
  try {
    const [attRes, setRes] = await Promise.all([
      questionSetsApi.getAttempts(setId),
      questionSetsApi.getById(setId)
    ])
    attempts.value = attRes.data
    setTitle.value = setRes.data.title
  } finally {
    loading.value = false
  }
})

async function openGrading(a) {
  selectedAttempt.value = a
  loadingDetail.value = true
  attemptDetail.value = null
  try {
    const { data } = await questionSetsApi.getAttemptDetail(setId, a.attemptId)
    attemptDetail.value = data
    for (const ans of data.answers) {
      grades[ans.answerId] = { points: ans.earnedPoints ?? 0, feedback: ans.feedback ?? '' }
    }
  } finally {
    loadingDetail.value = false
  }
}

async function saveGrade(ans) {
  savingGrade.value = ans.answerId
  try {
    await questionSetsApi.gradeEssay(selectedAttempt.value.attemptId, ans.questionId, {
      points: grades[ans.answerId].points,
      feedback: grades[ans.answerId].feedback
    })
    // update local state
    const local = attemptDetail.value.answers.find(a => a.answerId === ans.answerId)
    if (local) {
      local.earnedPoints = grades[ans.answerId].points
      local.feedback = grades[ans.answerId].feedback
      local.needsGrading = false
      local.isCorrect = grades[ans.answerId].points > 0
    }
    // refresh attempts list
    const idx = attempts.value.findIndex(a => a.attemptId === selectedAttempt.value.attemptId)
    if (idx !== -1) {
      const { data } = await questionSetsApi.getAttempts(setId)
      attempts.value = data
      selectedAttempt.value = data.find(a => a.attemptId === selectedAttempt.value.attemptId)
    }
  } finally {
    savingGrade.value = null
  }
}

function formatDate(d) {
  if (!d) return '-'
  return new Date(d).toLocaleString('id-ID', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

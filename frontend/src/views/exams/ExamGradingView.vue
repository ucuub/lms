<template>
  <div class="max-w-5xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <div>
        <h1 class="text-xl font-bold">Penilaian Ujian</h1>
        <p v-if="examTitle" class="text-sm text-gray-500">{{ examTitle }}</p>
      </div>
    </div>

    <div v-if="loading" class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <div v-else>
      <!-- Stats -->
      <div class="grid grid-cols-4 gap-4 mb-6">
        <div class="card p-4 text-center">
          <p class="text-2xl font-bold text-gray-900">{{ attempts.length }}</p>
          <p class="text-xs text-gray-500">Total Peserta</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-2xl font-bold text-green-600">{{ passCount }}</p>
          <p class="text-xs text-gray-500">Lulus</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-2xl font-bold text-red-500">{{ attempts.length - passCount }}</p>
          <p class="text-xs text-gray-500">Tidak Lulus</p>
        </div>
        <div class="card p-4 text-center">
          <p class="text-2xl font-bold text-amber-500">{{ ungradedCount }}</p>
          <p class="text-xs text-gray-500">Essay Belum Dinilai</p>
        </div>
      </div>

      <!-- Filter -->
      <div class="flex gap-3 mb-4">
        <button v-for="f in filters" :key="f.value"
          @click="activeFilter = f.value"
          :class="['btn-sm', activeFilter === f.value ? 'btn-primary' : 'btn-outline']">
          {{ f.label }}
        </button>
      </div>

      <div v-if="filteredAttempts.length === 0" class="card p-12 text-center text-gray-400">
        Belum ada peserta.
      </div>

      <!-- Daftar Peserta -->
      <div class="space-y-3">
        <div v-for="att in filteredAttempts" :key="att.attemptId" class="card overflow-hidden">
          <!-- Header peserta -->
          <div class="flex items-center justify-between p-4 cursor-pointer hover:bg-gray-50"
            @click="toggle(att.attemptId)">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-full bg-blue-100 text-blue-600 flex items-center justify-center text-sm font-bold shrink-0">
                {{ att.userName?.charAt(0).toUpperCase() }}
              </div>
              <div>
                <p class="font-medium text-gray-900 text-sm">{{ att.userName }}</p>
                <p class="text-xs text-gray-500">
                  {{ new Date(att.submittedAt).toLocaleString('id-ID') }}
                </p>
              </div>
            </div>
            <div class="flex items-center gap-3">
              <span v-if="att.hasUngradedEssay" class="badge-yellow badge text-xs">Essay belum dinilai</span>
              <span :class="['text-sm font-bold', att.isPassed ? 'text-green-600' : 'text-red-500']">
                {{ att.percentage }}%
              </span>
              <span class="text-xs text-gray-400">{{ att.score }}/{{ att.maxScore }}</span>
              <span :class="['badge text-xs', att.isPassed ? 'badge-green' : 'badge-red']">
                {{ att.isPassed ? 'Lulus' : 'Tidak Lulus' }}
              </span>
              <svg class="w-4 h-4 text-gray-400 transition-transform" :class="{ 'rotate-180': expanded[att.attemptId] }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"/>
              </svg>
            </div>
          </div>

          <!-- Detail jawaban (expanded) -->
          <div v-if="expanded[att.attemptId]" class="border-t border-gray-100 p-4">
            <div v-if="detailLoading[att.attemptId]" class="flex justify-center py-4">
              <div class="animate-spin rounded-full h-6 w-6 border-2 border-blue-600 border-t-transparent"></div>
            </div>

            <div v-else-if="details[att.attemptId]" class="space-y-4">
              <div v-for="(ans, idx) in details[att.attemptId].answers" :key="ans.questionId"
                :class="['p-4 rounded-lg border', answerBorderClass(ans)]">
                <div class="flex items-start gap-3">
                  <span class="text-xs font-bold text-gray-500 mt-0.5 w-5 shrink-0">{{ idx + 1 }}</span>
                  <div class="flex-1">
                    <div class="flex items-center gap-2 mb-1">
                      <span class="badge-gray badge text-xs">{{ typeLabel(ans.questionType) }}</span>
                      <span class="text-xs text-gray-400">{{ ans.points }} poin</span>
                    </div>
                    <p class="text-sm font-medium text-gray-800 mb-2">{{ ans.questionText }}</p>

                    <!-- Pilihan ganda / benar salah -->
                    <div v-if="ans.questionType !== 'Essay'" class="text-sm space-y-1">
                      <p>
                        <span class="text-gray-500">Jawaban: </span>
                        <span :class="ans.isCorrect ? 'text-green-700 font-medium' : 'text-red-600'">
                          {{ ans.selectedAnswer || '(tidak dijawab)' }}
                        </span>
                        <span v-if="ans.isCorrect" class="ml-2 text-green-500">✓</span>
                        <span v-else class="ml-2 text-red-400">✗</span>
                      </p>
                      <p v-if="!ans.isCorrect && ans.correctAnswer" class="text-gray-500">
                        Benar: <span class="text-green-700">{{ ans.correctAnswer }}</span>
                      </p>
                    </div>

                    <!-- Essay + form penilaian -->
                    <div v-else>
                      <p class="text-gray-500 text-xs mb-1">Jawaban essay:</p>
                      <p class="text-sm text-gray-700 whitespace-pre-line bg-white p-3 rounded border border-gray-200 mb-3">
                        {{ ans.essayAnswer || '(tidak dijawab)' }}
                      </p>

                      <div v-if="ans.needsGrading || gradeForm[`${att.attemptId}-${ans.questionId}`]"
                        class="flex items-end gap-3 bg-yellow-50 p-3 rounded border border-yellow-200">
                        <div>
                          <label class="label text-xs">Nilai (maks {{ ans.points }})</label>
                          <input type="number" :min="0" :max="ans.points"
                            v-model.number="gradeInputs[`${att.attemptId}-${ans.questionId}`].points"
                            class="input w-24" />
                        </div>
                        <div class="flex-1">
                          <label class="label text-xs">Feedback (opsional)</label>
                          <input type="text"
                            v-model="gradeInputs[`${att.attemptId}-${ans.questionId}`].feedback"
                            class="input" placeholder="Tulis feedback..." />
                        </div>
                        <button @click="gradeEssay(att.attemptId, ans)"
                          :disabled="grading[`${att.attemptId}-${ans.questionId}`]"
                          class="btn-primary btn-sm shrink-0">
                          {{ grading[`${att.attemptId}-${ans.questionId}`] ? 'Menyimpan...' : 'Simpan Nilai' }}
                        </button>
                      </div>

                      <div v-else class="text-sm text-green-600">
                        ✓ Nilai: {{ ans.earnedPoints }}/{{ ans.points }} poin
                        <span v-if="ans.feedback" class="text-gray-500"> · {{ ans.feedback }}</span>
                        <button @click="gradeForm[`${att.attemptId}-${ans.questionId}`] = true"
                          class="ml-2 text-blue-500 hover:underline text-xs">Ubah</button>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { examsApi } from '@/api/exams'

const route = useRoute()
const loading = ref(true)
const attempts = ref([])
const examTitle = ref('')
const expanded = reactive({})
const details = reactive({})
const detailLoading = reactive({})
const gradeInputs = reactive({})
const gradeForm = reactive({})
const grading = reactive({})

const activeFilter = ref('all')
const filters = [
  { value: 'all', label: 'Semua' },
  { value: 'ungraded', label: 'Essay Belum Dinilai' },
  { value: 'passed', label: 'Lulus' },
  { value: 'failed', label: 'Tidak Lulus' }
]

const passCount = computed(() => attempts.value.filter(a => a.isPassed).length)
const ungradedCount = computed(() => attempts.value.filter(a => a.hasUngradedEssay).length)
const filteredAttempts = computed(() => {
  if (activeFilter.value === 'ungraded') return attempts.value.filter(a => a.hasUngradedEssay)
  if (activeFilter.value === 'passed') return attempts.value.filter(a => a.isPassed)
  if (activeFilter.value === 'failed') return attempts.value.filter(a => !a.isPassed)
  return attempts.value
})

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}

function answerBorderClass(ans) {
  if (ans.questionType === 'Essay') {
    return ans.needsGrading ? 'border-yellow-200 bg-yellow-50' : 'border-green-200 bg-green-50'
  }
  return ans.isCorrect ? 'border-green-200 bg-green-50' : 'border-red-200 bg-red-50'
}

function getAnswerId(attemptId, questionId) {
  const detail = details[attemptId]
  if (!detail) return null
  // Cari answer id dari detail — kita simpan di reactive map
  return answerIds[`${attemptId}-${questionId}`]
}

async function toggle(attemptId) {
  expanded[attemptId] = !expanded[attemptId]
  if (expanded[attemptId] && !details[attemptId]) {
    detailLoading[attemptId] = true
    try {
      const { data } = await examsApi.getAttemptDetail(attemptId)
      details[attemptId] = data

      // Init grade inputs untuk soal essay
      data.answers.forEach(ans => {
        const key = `${attemptId}-${ans.questionId}`
        if (ans.questionType === 'Essay') {
          gradeInputs[key] = {
            points: ans.earnedPoints ?? 0,
            feedback: ans.feedback ?? ''
          }
        }
      })
    } finally {
      detailLoading[attemptId] = false
    }
  }
}

async function gradeEssay(attemptId, ans) {
  const key = `${attemptId}-${ans.questionId}`
  grading[key] = true
  try {
    await examsApi.gradeEssay(ans.answerId, gradeInputs[key])

    // Update local state
    const ansObj = detail.answers.find(a => a.questionId === ans.questionId)
    if (ansObj) {
      ansObj.earnedPoints = gradeInputs[key].points
      ansObj.feedback = gradeInputs[key].feedback
      ansObj.needsGrading = false
      ansObj.isCorrect = gradeInputs[key].points > 0
    }
    gradeForm[key] = false

    // Update summary
    const summary = attempts.value.find(a => a.attemptId === attemptId)
    if (summary) {
      const stillUngraded = detail.answers.some(a => a.questionType === 'Essay' && a.needsGrading)
      summary.hasUngradedEssay = stillUngraded
    }
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal menyimpan nilai.')
  } finally {
    grading[key] = false
  }
}

onMounted(async () => {
  try {
    const { data: exam } = await examsApi.getById(route.params.id)
    examTitle.value = exam.title

    const { data } = await examsApi.getAttempts(route.params.id)
    attempts.value = data
  } finally {
    loading.value = false
  }
})
</script>

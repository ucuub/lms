<template>
  <!-- Standalone page — no AppLayout, authenticated by exam token -->
  <div class="min-h-screen bg-gray-50">

    <!-- Loading -->
    <div v-if="state === 'loading'" class="min-h-screen flex items-center justify-center">
      <div class="text-center">
        <div class="w-10 h-10 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
        <p class="text-gray-500">Memvalidasi akses...</p>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="state === 'error'" class="min-h-screen flex items-center justify-center p-6">
      <div class="bg-white rounded-2xl shadow-lg p-8 max-w-md w-full text-center">
        <div class="w-16 h-16 bg-red-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg class="w-8 h-8 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>
          </svg>
        </div>
        <h2 class="text-xl font-bold text-gray-900 mb-2">Akses Ditolak</h2>
        <p class="text-gray-500 text-sm">{{ errorMsg }}</p>
      </div>
    </div>

    <!-- Exam -->
    <div v-else-if="state === 'exam'" class="max-w-3xl mx-auto px-4 py-6">
      <!-- Header -->
      <div class="bg-white rounded-xl border border-gray-200 p-5 mb-5 flex items-center justify-between gap-4">
        <div>
          <h1 class="text-xl font-bold text-gray-900">{{ session.examTitle }}</h1>
          <p v-if="session.examDescription" class="text-sm text-gray-500 mt-0.5">{{ session.examDescription }}</p>
          <p v-if="session.isResume" class="text-xs text-blue-600 mt-1 font-medium">Melanjutkan sesi sebelumnya</p>
        </div>
        <!-- Timer -->
        <div v-if="session.timeLimitMinutes" class="text-center shrink-0">
          <div :class="timeLeft <= 300 ? 'text-red-600' : 'text-gray-800'"
               class="text-2xl font-mono font-bold">{{ formatTime(timeLeft) }}</div>
          <p class="text-xs text-gray-400">tersisa</p>
        </div>
      </div>

      <!-- Progress -->
      <div class="mb-4 text-sm text-gray-500">
        Soal {{ currentIdx + 1 }} dari {{ session.questions.length }}
      </div>

      <!-- Questions -->
      <div class="space-y-5">
        <div v-for="(q, idx) in session.questions" :key="q.id"
          class="bg-white rounded-xl border border-gray-200 p-5">
          <p class="font-medium text-gray-800 mb-3">{{ idx + 1 }}. {{ q.text }}</p>
          <p class="text-xs text-gray-400 mb-3">{{ q.points }} poin · {{ q.type }}</p>

          <!-- Multiple Choice / True False -->
          <div v-if="q.type !== 'Essay'" class="space-y-2">
            <label v-for="opt in q.options" :key="opt.id"
              class="flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors"
              :class="answers[q.id]?.selectedOptionId === opt.id
                ? 'border-blue-500 bg-blue-50'
                : 'border-gray-200 hover:border-gray-300'">
              <input type="radio" :name="`q_${q.id}`" :value="opt.id"
                v-model="answers[q.id].selectedOptionId" class="accent-blue-600" />
              <span class="text-sm text-gray-700">{{ opt.text }}</span>
            </label>
          </div>

          <!-- Essay -->
          <div v-else>
            <textarea v-model="answers[q.id].essayAnswer" rows="4"
              class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              placeholder="Tulis jawaban Anda di sini..."></textarea>
          </div>
        </div>
      </div>

      <!-- Submit -->
      <div class="mt-6 flex justify-end">
        <button @click="confirmSubmit" :disabled="submitting"
          class="px-6 py-3 bg-blue-600 text-white font-semibold rounded-xl hover:bg-blue-700 disabled:opacity-50 transition-colors">
          {{ submitting ? 'Mengirim...' : 'Selesai & Kirim Jawaban' }}
        </button>
      </div>
    </div>

    <!-- Result -->
    <div v-else-if="state === 'result'" class="max-w-3xl mx-auto px-4 py-8">
      <div class="bg-white rounded-2xl border border-gray-200 p-8 text-center mb-6">
        <div :class="result.isPassed ? 'bg-green-100' : 'bg-red-100'"
          class="w-20 h-20 rounded-full flex items-center justify-center mx-auto mb-4">
          <svg v-if="result.isPassed" class="w-10 h-10 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <svg v-else class="w-10 h-10 text-red-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
        </div>
        <h2 class="text-2xl font-bold mb-1" :class="result.isPassed ? 'text-green-700' : 'text-red-600'">
          {{ result.isPassed ? 'Lulus!' : 'Belum Lulus' }}
        </h2>
        <p class="text-gray-500 mb-4">{{ result.examTitle }}</p>
        <div class="text-5xl font-bold mb-2" :class="result.isPassed ? 'text-green-600' : 'text-red-500'">
          {{ result.percentage }}%
        </div>
        <p class="text-sm text-gray-500">
          Skor: {{ result.score }} / {{ result.maxScore }} · Minimum lulus: {{ result.passScore }}%
        </p>
        <div class="flex items-center justify-center gap-3 mt-2 text-xs text-gray-400 flex-wrap">
          <span>Percobaan {{ result.maxAttempts - result.remainingAttempts }} dari {{ result.maxAttempts }}</span>
          <span>·</span>
          <span>Durasi: {{ examDuration }}</span>
        </div>
        <!-- Essay pending note -->
        <div v-if="hasEssay" class="mt-3 text-xs text-yellow-700 bg-yellow-50 border border-yellow-200 rounded-lg px-3 py-2">
          Ada soal essay yang menunggu penilaian guru. Skor akhir mungkin berubah setelah dinilai.
        </div>

        <!-- Retry button -->
        <div v-if="!result.isPassed && result.remainingAttempts > 0" class="mt-4">
          <button @click="retryExam"
            class="px-5 py-2 bg-blue-600 text-white text-sm font-semibold rounded-xl hover:bg-blue-700 transition-colors">
            Coba Lagi ({{ result.remainingAttempts }} percobaan tersisa)
          </button>
        </div>
        <div v-else-if="!result.isPassed && result.remainingAttempts === 0" class="mt-3">
          <p class="text-xs text-red-500 font-medium">Percobaan sudah habis.</p>
        </div>
      </div>

      <!-- Answer breakdown -->
      <div class="space-y-3">
        <h3 class="font-semibold text-gray-800 mb-2">Pembahasan Jawaban</h3>
        <div v-for="(ans, idx) in result.answers" :key="ans.questionId"
          class="bg-white rounded-xl border p-4"
          :class="ans.questionType === 'Essay' ? 'border-yellow-200' :
                  ans.isCorrect ? 'border-green-200' : 'border-red-200'">
          <p class="text-sm font-medium text-gray-800 mb-2">{{ idx + 1 }}. {{ ans.questionText }}</p>

          <div v-if="ans.questionType === 'Essay'" class="text-sm">
            <p class="text-gray-500 text-xs mb-1">Jawaban Anda:</p>
            <p class="text-gray-700 bg-gray-50 rounded p-2">{{ ans.essayAnswer || '(tidak dijawab)' }}</p>
            <p v-if="ans.feedback" class="text-xs text-blue-600 mt-1">Feedback: {{ ans.feedback }}</p>
            <p class="text-xs text-yellow-600 mt-1">Menunggu penilaian guru</p>
          </div>
          <div v-else class="text-sm space-y-1">
            <div class="flex gap-2">
              <span class="text-gray-500">Jawaban Anda:</span>
              <span :class="ans.isCorrect ? 'text-green-600 font-medium' : 'text-red-500'">
                {{ ans.selectedOptionText || '(tidak dijawab)' }}
              </span>
            </div>
            <div v-if="!ans.isCorrect && ans.correctAnswer" class="flex gap-2">
              <span class="text-gray-500">Jawaban benar:</span>
              <span class="text-green-600 font-medium">{{ ans.correctAnswer }}</span>
            </div>
            <p class="text-xs text-gray-400">{{ ans.earnedPoints ?? 0 }} / {{ ans.points }} poin</p>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted, computed } from 'vue'


import { useRoute } from 'vue-router'
import { mandatoryExamSession } from '@/api/mandatoryExam'

const route  = useRoute()

// token bisa dari URL (?token=) atau dari response accessByCode (?code=)
const examToken = ref(route.query.token || '')

const state      = ref('loading') // loading | error | exam | result
const errorMsg   = ref('')
const session    = ref(null)      // ValidateMandatoryTokenResponse / PublicAccessExamResponse
const result     = ref(null)      // MandatoryExamResultResponse
const submitting = ref(false)

// answers keyed by questionId
const answers  = reactive({})
const timeLeft = ref(0)
const currentIdx = ref(0)
let   timer    = null

async function startExam() {
  const tokenParam    = route.query.token
  const codeParam     = route.query.code
  const userIdParam   = route.query.userId
  const userNameParam = route.query.userName

  state.value = 'loading'

  try {
    let data

    if (codeParam) {
      if (!userIdParam) {
        errorMsg.value = 'Parameter userId diperlukan. Pastikan link dibuka melalui aplikasi DWI Mobile.'
        state.value    = 'error'
        return
      }
      const res = await mandatoryExamSession.accessByCode(codeParam, userIdParam, userNameParam)
      data = res.data
      examToken.value = data.examToken

    } else if (tokenParam) {
      examToken.value = tokenParam
      const res = await mandatoryExamSession.validateToken(tokenParam)
      data = res.data

    } else {
      errorMsg.value = 'Link ujian tidak valid.'
      state.value    = 'error'
      return
    }

    session.value = data

    // Reset & init answer slots
    Object.keys(answers).forEach(k => delete answers[k])
    for (const q of data.questions) {
      answers[q.id] = { selectedOptionId: null, essayAnswer: '' }
    }

    // Timer
    if (data.timeLimitMinutes) {
      const elapsed = Math.floor((Date.now() - new Date(data.startedAt).getTime()) / 1000)
      timeLeft.value = Math.max(0, data.timeLimitMinutes * 60 - elapsed)
      if (timeLeft.value > 0) {
        timer = setInterval(() => {
          timeLeft.value--
          if (timeLeft.value <= 0) {
            clearInterval(timer)
            submitExam()
          }
        }, 1000)
      } else {
        submitExam()
        return
      }
    }

    state.value = 'exam'
  } catch (e) {
    const msg = e?.response?.data?.message ?? e.message
    errorMsg.value = msg || 'Token tidak valid atau sudah expired.'
    state.value    = 'error'
  }
}

async function retryExam() {
  if (timer) clearInterval(timer)
  result.value  = null
  session.value = null
  await startExam()
}

onMounted(startExam)

onUnmounted(() => { if (timer) clearInterval(timer) })

const examDuration = computed(() => {
  if (!result.value) return ''
  const ms = new Date(result.value.submittedAt) - new Date(result.value.startedAt)
  const totalMin = Math.floor(ms / 60000)
  const secs     = Math.floor((ms % 60000) / 1000)
  return totalMin > 0 ? `${totalMin} menit ${secs} detik` : `${secs} detik`
})

const hasEssay = computed(() =>
  result.value?.answers?.some(a => a.questionType === 'Essay') ?? false
)

function formatTime(secs) {
  const m = String(Math.floor(secs / 60)).padStart(2, '0')
  const s = String(secs % 60).padStart(2, '0')
  return `${m}:${s}`
}

async function confirmSubmit() {
  const unanswered = session.value.questions.filter(q => {
    const a = answers[q.id]
    if (q.type === 'Essay') return !a?.essayAnswer?.trim()
    return !a?.selectedOptionId
  })

  if (unanswered.length > 0) {
    if (!confirm(`Masih ada ${unanswered.length} soal yang belum dijawab. Lanjutkan?`)) return
  } else {
    if (!confirm('Kirim jawaban? Tidak bisa diubah setelah dikirim.')) return
  }

  await submitExam()
}

async function submitExam() {
  if (submitting.value) return
  submitting.value = true
  if (timer) clearInterval(timer)

  try {
    const payload = {
      answers: session.value.questions.map(q => ({
        questionId:       q.id,
        selectedOptionId: answers[q.id]?.selectedOptionId ?? null,
        essayAnswer:      answers[q.id]?.essayAnswer || null,
      }))
    }

    const { data } = await mandatoryExamSession.submit(session.value.attemptId, payload, examToken.value)
    result.value = data
    state.value  = 'result'
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengirim jawaban. Coba lagi.')
  } finally {
    submitting.value = false
  }
}
</script>

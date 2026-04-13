<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Loading -->
    <div v-if="loading" class="flex items-center justify-center h-screen">
      <div class="text-center">
        <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent mx-auto mb-4"></div>
        <p class="text-gray-500">Menyiapkan soal...</p>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="flex items-center justify-center h-screen p-6">
      <div class="card p-8 text-center max-w-sm">
        <p class="text-red-600 mb-4">{{ error }}</p>
        <RouterLink to="/practice" class="btn-outline btn-sm">Kembali</RouterLink>
      </div>
    </div>

    <!-- Quiz -->
    <template v-else>
      <!-- Top Bar -->
      <div class="sticky top-0 z-10 bg-white border-b border-gray-200 px-4 py-3">
        <div class="max-w-3xl mx-auto flex items-center justify-between">
          <div class="flex items-center gap-3">
            <span class="font-semibold text-gray-900 text-sm truncate max-w-48">
              {{ session.quizTitle }}
            </span>
            <span class="text-xs text-gray-400">
              {{ answeredCount }}/{{ questions.length }} dijawab
            </span>
          </div>
          <div class="flex items-center gap-3">
            <!-- Timer -->
            <div v-if="session.timeLimitMinutes" :class="[
              'font-mono text-sm font-semibold px-3 py-1 rounded-full',
              timeLeft <= 60 ? 'bg-red-100 text-red-700 animate-pulse'
              : timeLeft <= 300 ? 'bg-yellow-100 text-yellow-700'
              : 'bg-gray-100 text-gray-700'
            ]">
              {{ formatTime(timeLeft) }}
            </div>
            <button @click="confirmSubmit" :disabled="submitting"
              class="btn-primary btn-sm">
              {{ submitting ? 'Mengirim...' : 'Submit' }}
            </button>
          </div>
        </div>
      </div>

      <div class="max-w-3xl mx-auto p-4 pb-24">
        <!-- Progress -->
        <div class="mb-4">
          <div class="flex justify-between text-xs text-gray-400 mb-1">
            <span>Soal {{ currentIdx + 1 }} dari {{ questions.length }}</span>
            <span>{{ Math.round((answeredCount / questions.length) * 100) }}% selesai</span>
          </div>
          <div class="bg-gray-100 rounded-full h-1.5">
            <div class="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
              :style="`width: ${(answeredCount / questions.length) * 100}%`"></div>
          </div>
        </div>

        <!-- Question Card -->
        <div class="card p-6 mb-4">
          <div class="flex items-center gap-2 mb-4">
            <span class="w-7 h-7 rounded-full bg-blue-600 text-white text-xs font-bold flex items-center justify-center shrink-0">
              {{ currentIdx + 1 }}
            </span>
            <span class="badge badge-blue text-xs">{{ currentQ.type }}</span>
            <span class="text-xs text-gray-400">{{ currentQ.points }} poin</span>
          </div>

          <p class="text-gray-900 font-medium mb-5 leading-relaxed">{{ currentQ.text }}</p>

          <!-- Multiple Choice / True False -->
          <div v-if="currentQ.options" class="space-y-2">
            <label v-for="opt in currentQ.options" :key="opt.id"
              :class="[
                'flex items-center gap-3 p-3.5 rounded-xl border-2 cursor-pointer transition-all',
                answers[currentQ.id] === opt.id
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-100 hover:border-gray-300 hover:bg-gray-50'
              ]">
              <input type="radio"
                :name="`q-${currentQ.id}`"
                :value="opt.id"
                v-model="answers[currentQ.id]"
                class="shrink-0" />
              <span class="text-sm text-gray-800">{{ opt.text }}</span>
            </label>
          </div>

          <!-- Essay -->
          <div v-else>
            <textarea
              v-model="essayAnswers[currentQ.id]"
              class="textarea"
              rows="5"
              placeholder="Tulis jawaban kamu di sini...">
            </textarea>
            <p class="text-xs text-gray-400 mt-2">
              Essay dinilai secara terpisah. Pastikan kamu mengisi jawaban.
            </p>
          </div>
        </div>

        <!-- Navigation -->
        <div class="flex items-center justify-between gap-3">
          <button @click="currentIdx--" :disabled="currentIdx === 0"
            class="btn-outline btn-sm flex items-center gap-1">
            ← Sebelumnya
          </button>

          <!-- Dot Navigator -->
          <div class="flex gap-1 flex-wrap justify-center">
            <button v-for="(q, i) in questions" :key="q.id"
              @click="currentIdx = i"
              :class="[
                'w-7 h-7 rounded-full text-xs font-medium transition-all',
                i === currentIdx
                  ? 'bg-blue-600 text-white'
                  : isAnswered(q)
                    ? 'bg-green-100 text-green-700 hover:bg-green-200'
                    : 'bg-gray-100 text-gray-500 hover:bg-gray-200'
              ]">
              {{ i + 1 }}
            </button>
          </div>

          <button @click="currentIdx++" :disabled="currentIdx === questions.length - 1"
            class="btn-primary btn-sm flex items-center gap-1">
            Berikutnya →
          </button>
        </div>

        <!-- Unanswered Warning -->
        <div v-if="unansweredCount > 0 && showWarning"
          class="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg text-sm text-yellow-800">
          <strong>{{ unansweredCount }} soal belum dijawab.</strong>
          Kamu tetap bisa submit, soal yang belum dijawab akan dihitung salah.
        </div>
      </div>
    </template>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { practiceApi } from '@/api/practice'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const error = ref('')
const submitting = ref(false)
const showWarning = ref(false)

const session = ref(null)     // StartPracticeResponse
const questions = ref([])     // PracticeQuestionDto[]
const currentIdx = ref(0)
const answers = ref({})       // { [questionId]: selectedOptionId }
const essayAnswers = ref({})  // { [questionId]: text (not graded auto) }
const timeLeft = ref(0)
let timerInterval = null

const currentQ = computed(() => questions.value[currentIdx.value] ?? null)

const answeredCount = computed(() =>
  questions.value.filter(q => isAnswered(q)).length
)
const unansweredCount = computed(() =>
  questions.value.length - answeredCount.value
)

function isAnswered(q) {
  if (q.options) return answers.value[q.id] != null
  return !!essayAnswers.value[q.id]?.trim()
}

function formatTime(secs) {
  const m = Math.floor(secs / 60).toString().padStart(2, '0')
  const s = (secs % 60).toString().padStart(2, '0')
  return `${m}:${s}`
}

function startTimer(minutes) {
  timeLeft.value = minutes * 60
  timerInterval = setInterval(() => {
    if (timeLeft.value <= 0) {
      clearInterval(timerInterval)
      doSubmit()
      return
    }
    timeLeft.value--
  }, 1000)
}

function buildAnswers() {
  return questions.value.map(q => ({
    questionId: q.id,
    selectedOptionId: q.options
      ? (answers.value[q.id] ?? null)
      : null  // essay → no option
  }))
}

async function doSubmit() {
  submitting.value = true
  clearInterval(timerInterval)
  try {
    const ans = buildAnswers()
    await practiceApi.submit(session.value.attemptId, ans)
    router.push(`/practice/result/${session.value.attemptId}`)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal submit. Coba lagi.')
    submitting.value = false
  }
}

function confirmSubmit() {
  showWarning.value = unansweredCount.value > 0
  if (unansweredCount.value > 0) {
    if (!confirm(
      `${unansweredCount.value} soal belum dijawab. Submit sekarang?`
    )) return
  }
  doSubmit()
}

onMounted(async () => {
  const attemptId = route.params.attemptId
  try {
    // Coba ambil soal dari state atau fetch ulang dari session storage
    const stored = sessionStorage.getItem(`practice_session_${attemptId}`)
    if (stored) {
      const parsed = JSON.parse(stored)
      session.value = parsed
      questions.value = parsed.questions
    } else {
      // Attempt sudah ada — redirect ke result jika sudah submitted
      error.value = 'Sesi tidak ditemukan. Mulai ulang dari halaman Practice Quiz.'
      loading.value = false
      return
    }
    if (session.value.timeLimitMinutes) {
      startTimer(session.value.timeLimitMinutes)
    }
  } catch {
    error.value = 'Gagal memuat soal.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => clearInterval(timerInterval))
</script>

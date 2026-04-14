<template>
  <div class="max-w-3xl mx-auto p-6">
    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <!-- Belum mulai -->
    <div v-else-if="!attempt" class="card p-8 text-center">
      <h1 class="text-2xl font-bold text-gray-900 mb-2">{{ exam.title }}</h1>
      <p v-if="exam.description" class="text-gray-500 mb-6">{{ exam.description }}</p>
      <div class="flex justify-center gap-8 text-sm text-gray-600 mb-8">
        <div class="text-center">
          <p class="text-2xl font-bold text-gray-900">{{ exam.questionCount }}</p>
          <p>Soal</p>
        </div>
        <div class="text-center">
          <p class="text-2xl font-bold text-gray-900">{{ exam.timeLimitMinutes }}</p>
          <p>Menit</p>
        </div>
        <div class="text-center">
          <p class="text-2xl font-bold text-gray-900">{{ exam.passScore }}%</p>
          <p>Nilai Lulus</p>
        </div>
        <div class="text-center">
          <p class="text-2xl font-bold text-gray-900">{{ exam.totalPoints }}</p>
          <p>Total Poin</p>
        </div>
      </div>
      <p class="text-sm text-amber-600 mb-6">Setelah memulai, timer akan berjalan. Pastikan kamu siap!</p>
      <button @click="startExam" :disabled="starting" class="btn-primary px-8 py-3 text-base">
        {{ starting ? 'Memulai...' : 'Mulai Ujian' }}
      </button>
    </div>

    <!-- Sedang mengerjakan -->
    <div v-else>
      <!-- Header + Timer -->
      <div class="flex items-center justify-between mb-6 sticky top-0 bg-white border-b border-gray-200 py-3 -mx-6 px-6 z-10">
        <h1 class="font-bold text-gray-900">{{ attempt.title }}</h1>
        <div :class="['font-mono text-lg font-bold px-3 py-1 rounded', timeLeft <= 300 ? 'bg-red-100 text-red-600' : 'bg-blue-100 text-blue-700']">
          ⏱ {{ formattedTime }}
        </div>
      </div>

      <!-- Progress -->
      <div class="flex items-center gap-2 mb-6 text-sm text-gray-500">
        <span>{{ answeredCount }}/{{ attempt.questions.length }} dijawab</span>
        <div class="flex-1 h-1.5 bg-gray-200 rounded-full overflow-hidden">
          <div class="h-full bg-blue-500 rounded-full transition-all"
            :style="{ width: `${answeredCount / attempt.questions.length * 100}%` }"></div>
        </div>
      </div>

      <!-- Soal -->
      <div class="space-y-6">
        <div v-for="(q, idx) in attempt.questions" :key="q.id" class="card p-5">
          <div class="flex items-start gap-3 mb-4">
            <span class="w-7 h-7 rounded-full bg-blue-100 text-blue-700 text-sm font-bold flex items-center justify-center shrink-0">
              {{ idx + 1 }}
            </span>
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-1">
                <span class="badge-blue badge text-xs">{{ typeLabel(q.type) }}</span>
                <span class="text-xs text-gray-400">{{ q.points }} poin</span>
              </div>
              <p class="text-sm font-medium text-gray-800">{{ q.text }}</p>
            </div>
          </div>

          <!-- Pilihan Ganda / Benar-Salah -->
          <div v-if="q.type !== 'Essay'" class="space-y-2 ml-10">
            <label v-for="opt in q.options" :key="opt.id"
              :class="['flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition',
                answers[q.id]?.selectedOptionId === opt.id
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:bg-gray-50']">
              <input type="radio" :name="`q${q.id}`" :value="opt.id"
                v-model="answers[q.id].selectedOptionId" class="shrink-0" />
              <span class="text-sm">{{ opt.text }}</span>
            </label>
          </div>

          <!-- Essay -->
          <div v-else class="ml-10">
            <textarea v-model="answers[q.id].essayAnswer" class="textarea" rows="4"
              placeholder="Tulis jawaban kamu di sini..."></textarea>
          </div>
        </div>
      </div>

      <!-- Submit -->
      <div class="mt-6 flex justify-end">
        <button @click="submitExam" :disabled="submitting" class="btn-primary px-8 py-3">
          {{ submitting ? 'Mengumpulkan...' : 'Kumpulkan Ujian' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examsApi } from '@/api/exams'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const starting = ref(false)
const submitting = ref(false)
const exam = ref(null)
const attempt = ref(null)
const answers = reactive({})
let timerInterval = null
const timeLeft = ref(0)

const formattedTime = computed(() => {
  const m = Math.floor(timeLeft.value / 60).toString().padStart(2, '0')
  const s = (timeLeft.value % 60).toString().padStart(2, '0')
  return `${m}:${s}`
})

const answeredCount = computed(() => {
  if (!attempt.value) return 0
  return attempt.value.questions.filter(q => {
    const a = answers[q.id]
    return q.type === 'Essay' ? a?.essayAnswer?.trim() : a?.selectedOptionId != null
  }).length
})

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}

function initAnswers(questions) {
  questions.forEach(q => {
    answers[q.id] = { selectedOptionId: null, essayAnswer: '' }
  })
}

function startTimer(startedAt, limitMinutes) {
  const endTime = new Date(startedAt).getTime() + limitMinutes * 60 * 1000
  timerInterval = setInterval(() => {
    const remaining = Math.max(0, Math.floor((endTime - Date.now()) / 1000))
    timeLeft.value = remaining
    if (remaining === 0) {
      clearInterval(timerInterval)
      submitExam()
    }
  }, 1000)
}

async function startExam() {
  starting.value = true
  try {
    const { data } = await examsApi.start(route.params.id)
    attempt.value = data
    initAnswers(data.questions)
    if (data.timeLimitMinutes > 0) startTimer(data.startedAt, data.timeLimitMinutes)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal memulai ujian.')
  } finally {
    starting.value = false
  }
}

async function submitExam() {
  if (!confirm('Yakin ingin mengumpulkan ujian?')) return
  clearInterval(timerInterval)
  submitting.value = true
  try {
    const answerList = attempt.value.questions.map(q => ({
      questionId: q.id,
      selectedOptionId: answers[q.id]?.selectedOptionId ?? null,
      essayAnswer: answers[q.id]?.essayAnswer ?? null
    }))
    const { data } = await examsApi.submit(attempt.value.attemptId, answerList)
    router.push(`/exam-attempts/${data.attemptId}/result`)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal mengumpulkan ujian.')
  } finally {
    submitting.value = false
  }
}

onMounted(async () => {
  try {
    const { data } = await examsApi.getById(route.params.id)
    exam.value = data
  } finally {
    loading.value = false
  }
})

onUnmounted(() => clearInterval(timerInterval))
</script>

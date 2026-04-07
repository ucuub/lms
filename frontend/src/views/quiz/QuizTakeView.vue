<template>
  <div class="min-h-screen bg-gray-50 py-6">
    <div v-if="quiz" class="max-w-3xl mx-auto px-4">
      <!-- Header with timer -->
      <div class="card p-4 mb-6 flex items-center justify-between sticky top-4 z-10">
        <div>
          <h1 class="font-semibold text-gray-900">{{ quiz.quizTitle }}</h1>
          <p class="text-sm text-gray-500">{{ quiz.questions.length }} soal</p>
        </div>
        <div v-if="quiz.timeLimitMinutes > 0"
          :class="['flex items-center gap-2 px-4 py-2 rounded-lg font-mono text-lg font-bold', timeWarning ? 'bg-red-100 text-red-700' : 'bg-blue-50 text-blue-700']">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          {{ formattedTime }}
        </div>
      </div>

      <!-- Questions -->
      <div class="space-y-5">
        <div v-for="(q, idx) in quiz.questions" :key="q.id" class="card p-6">
          <div class="flex items-start gap-3 mb-4">
            <span class="w-7 h-7 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-sm font-bold shrink-0 mt-0.5">
              {{ idx + 1 }}
            </span>
            <p class="text-gray-900 font-medium leading-relaxed">{{ q.text }}</p>
          </div>

          <div class="ml-10">
            <!-- MCQ / TF -->
            <div v-if="q.type !== 'Essay'" class="space-y-2">
              <label v-for="opt in q.options" :key="opt.id"
                :class="['flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition',
                         answers[q.id]?.selectedOptionId === opt.id
                           ? 'border-blue-500 bg-blue-50'
                           : 'border-gray-200 hover:bg-gray-50']">
                <input type="radio" :name="`q-${q.id}`" :value="opt.id" v-model.number="answers[q.id].selectedOptionId" class="w-4 h-4 text-blue-600" />
                <span class="text-sm text-gray-800">{{ opt.text }}</span>
              </label>
            </div>

            <!-- Essay -->
            <div v-else>
              <textarea v-model="answers[q.id].essayAnswer" class="textarea" rows="5"
                placeholder="Tulis jawaban Anda di sini..."></textarea>
            </div>
          </div>
        </div>
      </div>

      <!-- Submit -->
      <div class="mt-6 card p-5 flex items-center justify-between">
        <div class="text-sm text-gray-600">
          <span class="font-medium">{{ answeredCount }}</span> / {{ quiz.questions.length }} soal dijawab
        </div>
        <button @click="confirmSubmit" :disabled="submitting" class="btn-primary px-8">
          {{ submitting ? 'Mengumpulkan...' : 'Kumpulkan' }}
        </button>
      </div>
    </div>

    <!-- Loading -->
    <div v-else-if="loading" class="flex justify-center items-center min-h-screen">
      <div class="animate-spin rounded-full h-12 w-12 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <!-- Confirm Modal -->
    <Teleport to="body">
      <div v-if="showConfirm" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
        <div class="card p-6 max-w-sm w-full">
          <h3 class="font-semibold text-gray-900 mb-2">Kumpulkan Quiz?</h3>
          <p class="text-sm text-gray-600 mb-4">
            Anda telah menjawab {{ answeredCount }} dari {{ quiz.questions.length }} soal.
            Jawaban yang belum diisi akan dianggap tidak dijawab.
          </p>
          <div class="flex gap-3">
            <button @click="submitQuiz" class="btn-primary flex-1">Ya, Kumpulkan</button>
            <button @click="showConfirm = false" class="btn-outline flex-1">Kembali</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { quizzesApi } from '@/api/quizzes'

const route = useRoute()
const router = useRouter()

const quiz = ref(null)
const loading = ref(true)
const submitting = ref(false)
const showConfirm = ref(false)
const answers = reactive({})
const timeLeft = ref(0)
let timer = null

const formattedTime = computed(() => {
  const m = Math.floor(timeLeft.value / 60).toString().padStart(2, '0')
  const s = (timeLeft.value % 60).toString().padStart(2, '0')
  return `${m}:${s}`
})

const timeWarning = computed(() => timeLeft.value > 0 && timeLeft.value <= 60)

const answeredCount = computed(() => {
  if (!quiz.value) return 0
  return quiz.value.questions.filter(q => {
    const a = answers[q.id]
    return a?.selectedOptionId != null || (a?.essayAnswer?.trim().length > 0)
  }).length
})

function initAnswers(questions) {
  questions.forEach(q => {
    answers[q.id] = { selectedOptionId: null, essayAnswer: '' }
  })
}

function startTimer(minutes) {
  timeLeft.value = minutes * 60
  timer = setInterval(() => {
    if (timeLeft.value <= 0) {
      clearInterval(timer)
      submitQuiz()
    } else {
      timeLeft.value--
    }
  }, 1000)
}

function confirmSubmit() {
  showConfirm.value = true
}

async function submitQuiz() {
  showConfirm.value = false
  submitting.value = true
  try {
    const answerList = quiz.value.questions.map(q => ({
      questionId: q.id,
      selectedOptionId: answers[q.id]?.selectedOptionId ?? null,
      essayAnswer: answers[q.id]?.essayAnswer || null
    }))
    await quizzesApi.submit(quiz.value.attemptId, answerList)
    router.push(`/attempts/${quiz.value.attemptId}/result`)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal mengumpulkan quiz.')
    submitting.value = false
  }
}

onMounted(async () => {
  try {
    const { data } = await quizzesApi.start(route.params.id)
    quiz.value = data
    initAnswers(data.questions)
    if (data.timeLimitMinutes > 0) startTimer(data.timeLimitMinutes)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal memulai quiz.')
    router.back()
  } finally {
    loading.value = false
  }
})

onUnmounted(() => clearInterval(timer))
</script>

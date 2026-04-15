<template>
  <div class="min-h-screen bg-gray-50 py-6 px-4">
    <!-- Loading -->
    <div v-if="loading" class="max-w-2xl mx-auto text-center py-20 text-gray-400">Memuat soal...</div>

    <!-- Error -->
    <div v-else-if="error" class="max-w-2xl mx-auto text-center py-20">
      <p class="text-red-500 mb-4">{{ error }}</p>
      <RouterLink to="/ujian" class="text-blue-600 hover:underline text-sm">Kembali ke Daftar</RouterLink>
    </div>

    <!-- Submitted -->
    <div v-else-if="submitted" class="max-w-md mx-auto text-center py-20">
      <div class="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
        </svg>
      </div>
      <h2 class="text-xl font-bold text-gray-900 mb-2">Jawaban Dikumpulkan!</h2>
      <p class="text-gray-500 text-sm mb-6">Jawabanmu sudah berhasil disimpan.</p>
      <RouterLink :to="`/ujian-attempts/${attemptId}/result`"
        class="inline-block px-6 py-2.5 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 transition-colors">
        Lihat Hasil
      </RouterLink>
    </div>

    <!-- Quiz taking UI -->
    <div v-else-if="session" class="max-w-2xl mx-auto">
      <!-- Header -->
      <div class="bg-white rounded-xl border border-gray-200 p-4 mb-4 flex items-center justify-between">
        <div>
          <h1 class="font-bold text-gray-900">{{ session.title }}</h1>
          <p class="text-sm text-gray-500">{{ session.questions.length }} soal</p>
        </div>
        <div v-if="session.timeLimitMinutes" class="text-right">
          <div :class="timeLeft <= 60 ? 'text-red-500' : 'text-gray-700'"
            class="text-2xl font-mono font-bold">
            {{ formatTime(timeLeft) }}
          </div>
          <p class="text-xs text-gray-400">tersisa</p>
        </div>
      </div>

      <!-- Dot navigator -->
      <div class="bg-white rounded-xl border border-gray-200 p-3 mb-4 flex flex-wrap gap-2">
        <button v-for="(q, idx) in session.questions" :key="q.id"
          @click="currentIdx = idx"
          :class="[
            'w-8 h-8 rounded-full text-xs font-medium transition-colors',
            currentIdx === idx ? 'bg-blue-600 text-white' :
            answers[q.id]?.selectedOptionId || answers[q.id]?.essayAnswer ? 'bg-green-100 text-green-700' :
            'bg-gray-100 text-gray-500 hover:bg-gray-200'
          ]">
          {{ idx + 1 }}
        </button>
      </div>

      <!-- Question card -->
      <div v-if="currentQuestion" class="bg-white rounded-xl border border-gray-200 p-6 mb-4">
        <div class="flex items-start gap-3 mb-4">
          <span class="w-8 h-8 bg-blue-100 text-blue-700 rounded-full flex items-center justify-center text-sm font-bold shrink-0">
            {{ currentIdx + 1 }}
          </span>
          <div>
            <p class="text-gray-900 font-medium">{{ currentQuestion.text }}</p>
            <p class="text-xs text-gray-400 mt-1">{{ typeLabel(currentQuestion.type) }} · {{ currentQuestion.points }} poin</p>
          </div>
        </div>

        <!-- Options: MultipleChoice / TrueFalse -->
        <div v-if="currentQuestion.type !== 'Essay'" class="space-y-2 ml-11">
          <label v-for="opt in currentQuestion.options" :key="opt.id"
            class="flex items-center gap-3 p-3 border rounded-lg cursor-pointer transition-colors"
            :class="answers[currentQuestion.id]?.selectedOptionId === opt.id
              ? 'border-blue-400 bg-blue-50'
              : 'border-gray-200 hover:bg-gray-50'">
            <input type="radio" :name="`q${currentQuestion.id}`" :value="opt.id"
              v-model="answers[currentQuestion.id].selectedOptionId" class="text-blue-600" />
            <span class="text-sm text-gray-700">{{ opt.text }}</span>
          </label>
        </div>

        <!-- Essay -->
        <div v-else class="ml-11">
          <textarea v-model="answers[currentQuestion.id].essayAnswer"
            rows="5" placeholder="Tulis jawabanmu di sini..."
            class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none" />
        </div>
      </div>

      <!-- Navigation -->
      <div class="flex items-center justify-between">
        <button @click="currentIdx--" :disabled="currentIdx === 0"
          class="px-4 py-2 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 disabled:opacity-40 transition-colors">
          Sebelumnya
        </button>

        <span class="text-sm text-gray-500">{{ answeredCount }} / {{ session.questions.length }} dijawab</span>

        <div class="flex gap-2">
          <button v-if="currentIdx < session.questions.length - 1"
            @click="currentIdx++"
            class="px-4 py-2 text-sm font-medium bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors">
            Selanjutnya
          </button>
          <button v-else @click="confirmSubmit"
            class="px-4 py-2 text-sm font-medium bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors">
            Kumpulkan
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, reactive } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { questionSetsApi } from '@/api/questionSets'

const route = useRoute()
const router = useRouter()

const loading = ref(true)
const error = ref('')
const session = ref(null)
const attemptId = ref(null)
const submitted = ref(false)
const currentIdx = ref(0)
const answers = reactive({})
let timer = null
const timeLeft = ref(0)

const currentQuestion = computed(() => session.value?.questions[currentIdx.value])
const answeredCount = computed(() =>
  session.value?.questions.filter(q =>
    answers[q.id]?.selectedOptionId || answers[q.id]?.essayAnswer
  ).length ?? 0
)

onMounted(async () => {
  const id = Number(route.params.id)
  try {
    const { data } = await questionSetsApi.start(id)
    session.value = data
    attemptId.value = data.attemptId

    // Init answers
    for (const q of data.questions) {
      answers[q.id] = { questionId: q.id, selectedOptionId: null, essayAnswer: '' }
    }

    // Timer
    if (data.timeLimitMinutes) {
      timeLeft.value = data.timeLimitMinutes * 60
      timer = setInterval(() => {
        timeLeft.value--
        if (timeLeft.value <= 0) {
          clearInterval(timer)
          doSubmit()
        }
      }, 1000)
    }
  } catch (e) {
    error.value = e?.response?.data?.message ?? 'Gagal memuat soal.'
  } finally {
    loading.value = false
  }
})

onUnmounted(() => clearInterval(timer))

function formatTime(s) {
  const m = Math.floor(s / 60).toString().padStart(2, '0')
  const sec = (s % 60).toString().padStart(2, '0')
  return `${m}:${sec}`
}

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}

function confirmSubmit() {
  const unanswered = session.value.questions.length - answeredCount.value
  if (unanswered > 0) {
    if (!confirm(`Masih ada ${unanswered} soal yang belum dijawab. Kumpulkan sekarang?`)) return
  }
  doSubmit()
}

async function doSubmit() {
  clearInterval(timer)
  const answerList = Object.values(answers).map(a => ({
    questionId: a.questionId,
    selectedOptionId: a.selectedOptionId,
    essayAnswer: a.essayAnswer || null
  }))
  try {
    await questionSetsApi.submit(attemptId.value, { answers: answerList })
    submitted.value = true
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengumpulkan jawaban.')
  }
}
</script>

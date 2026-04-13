<template>
  <div class="max-w-4xl mx-auto p-6">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Practice Quiz</h1>
        <p class="text-sm text-gray-500 mt-1">
          Latihan soal acak dari bank soal — tanpa perlu enroll kursus
        </p>
      </div>
      <button v-if="auth.isTeacher" @click="showCreate = true" class="btn-primary btn-sm">
        + Buat Practice Quiz
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="card p-5 animate-pulse">
        <div class="h-4 bg-gray-200 rounded w-1/3 mb-2"></div>
        <div class="h-3 bg-gray-200 rounded w-2/3"></div>
      </div>
    </div>

    <!-- Empty -->
    <div v-else-if="quizzes.length === 0" class="card p-16 text-center">
      <div class="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
            d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
        </svg>
      </div>
      <p class="text-gray-500 mb-1">Belum ada practice quiz.</p>
      <p v-if="auth.isTeacher" class="text-sm text-gray-400">
        Buat practice quiz pertama dari bank soal yang sudah ada.
      </p>
    </div>

    <!-- Quiz List -->
    <div v-else class="space-y-4">
      <!-- My Attempts Section -->
      <div v-if="myAttempts.length > 0" class="mb-6">
        <h2 class="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
          Riwayat Terakhir Saya
        </h2>
        <div class="grid sm:grid-cols-2 gap-3">
          <RouterLink v-for="a in myAttempts.slice(0, 4)" :key="a.id"
            :to="a.isCompleted ? `/practice/result/${a.id}` : `/practice/attempt/${a.id}`"
            class="card p-4 hover:shadow-md transition-shadow flex items-center gap-3">
            <div :class="[
              'w-10 h-10 rounded-full flex items-center justify-center shrink-0 text-sm font-bold',
              a.isCompleted
                ? (a.score >= 70 ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700')
                : 'bg-yellow-100 text-yellow-700'
            ]">
              {{ a.isCompleted ? Math.round(a.score) + '%' : '...' }}
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-900 truncate">{{ a.quizTitle }}</p>
              <p class="text-xs text-gray-400">
                {{ a.isCompleted
                  ? `${a.correctAnswers}/${a.totalQuestions} benar · ${formatDate(a.submittedAt)}`
                  : 'Belum selesai'
                }}
              </p>
            </div>
          </RouterLink>
        </div>
      </div>

      <!-- Quiz Cards -->
      <h2 class="text-sm font-semibold text-gray-500 uppercase tracking-wider mb-3">
        Semua Practice Quiz
      </h2>
      <div class="grid sm:grid-cols-2 gap-4">
        <div v-for="q in quizzes" :key="q.id" class="card p-5 flex flex-col">
          <!-- Header -->
          <div class="flex items-start justify-between gap-2 mb-3">
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-1">
                <h3 class="font-semibold text-gray-900 text-base">{{ q.title }}</h3>
                <span v-if="!q.isActive" class="badge-gray badge text-xs">Nonaktif</span>
              </div>
              <p v-if="q.description" class="text-sm text-gray-500 line-clamp-2">
                {{ q.description }}
              </p>
            </div>
            <button v-if="auth.isTeacher"
              @click.stop="confirmDelete(q)"
              class="text-gray-300 hover:text-red-400 transition-colors shrink-0 ml-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
              </svg>
            </button>
          </div>

          <!-- Stats -->
          <div class="flex flex-wrap gap-3 text-xs text-gray-500 mb-4">
            <span class="flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              {{ q.questionCount }} soal acak
            </span>
            <span v-if="q.timeLimitMinutes" class="flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
              </svg>
              {{ q.timeLimitMinutes }} menit
            </span>
            <span class="flex items-center gap-1">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
              </svg>
              {{ q.shuffleQuestions ? 'Soal diacak' : 'Urut' }}
            </span>
            <span v-if="q.myAttemptCount > 0"
              class="flex items-center gap-1 text-blue-600 font-medium">
              {{ q.myAttemptCount }}x dikerjakan
            </span>
          </div>

          <!-- Action -->
          <div class="mt-auto">
            <button @click="startQuiz(q)" :disabled="starting === q.id || !q.isActive"
              class="btn-primary btn-sm w-full">
              <span v-if="starting === q.id">Menyiapkan soal...</span>
              <span v-else>{{ q.myAttemptCount > 0 ? 'Kerjakan Lagi' : 'Mulai' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      @click.self="showCreate = false">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
        <h3 class="font-semibold text-gray-900 mb-4">Buat Practice Quiz Baru</h3>
        <div class="space-y-4">
          <div>
            <label class="label">Judul *</label>
            <input v-model="form.title" class="input" placeholder="Contoh: Latihan Soal Pemrograman" />
          </div>
          <div>
            <label class="label">Deskripsi</label>
            <textarea v-model="form.description" class="textarea" rows="2"
              placeholder="Opsional — jelaskan topik atau tujuan latihan ini"></textarea>
          </div>
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Jumlah Soal</label>
              <input v-model.number="form.questionCount" type="number" class="input" min="1" max="100" />
              <p class="text-xs text-gray-400 mt-1">Dari bank soal yang ada</p>
            </div>
            <div>
              <label class="label">Batas Waktu (menit)</label>
              <input v-model.number="form.timeLimitMinutes" type="number" class="input"
                placeholder="Kosong = tanpa batas" min="1" />
            </div>
          </div>
          <div class="space-y-2">
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" v-model="form.shuffleQuestions" class="rounded" />
              <span class="text-sm text-gray-700">Acak urutan soal</span>
            </label>
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="checkbox" v-model="form.shuffleOptions" class="rounded" />
              <span class="text-sm text-gray-700">Acak urutan opsi jawaban</span>
            </label>
          </div>
          <div v-if="createError" class="text-sm text-red-600 bg-red-50 rounded-lg p-3">
            {{ createError }}
          </div>
        </div>
        <div class="flex gap-2 mt-5">
          <button @click="createQuiz" :disabled="creating || !form.title.trim()"
            class="btn-primary flex-1">
            {{ creating ? 'Membuat...' : 'Buat Quiz' }}
          </button>
          <button @click="showCreate = false" class="btn-outline">Batal</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { practiceApi } from '@/api/practice'

const auth = useAuthStore()
const router = useRouter()

const loading = ref(true)
const quizzes = ref([])
const myAttempts = ref([])

const showCreate = ref(false)
const creating = ref(false)
const createError = ref('')
const starting = ref(null)

const form = ref({
  title: '',
  description: '',
  questionCount: 10,
  shuffleQuestions: true,
  shuffleOptions: true,
  timeLimitMinutes: null
})

async function load() {
  try {
    const [listRes, attRes] = await Promise.all([
      practiceApi.getAll(),
      practiceApi.myAttempts()
    ])
    quizzes.value = listRes.data
    myAttempts.value = attRes.data
  } finally {
    loading.value = false
  }
}

async function startQuiz(quiz) {
  starting.value = quiz.id
  try {
    const { data } = await practiceApi.start(quiz.id)
    // Simpan soal ke sessionStorage agar PracticeTakeView bisa akses
    sessionStorage.setItem(`practice_session_${data.attemptId}`, JSON.stringify(data))
    router.push(`/practice/attempt/${data.attemptId}`)
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal memulai quiz.')
  } finally {
    starting.value = null
  }
}

async function createQuiz() {
  createError.value = ''
  creating.value = true
  try {
    await practiceApi.create({
      title: form.value.title.trim(),
      description: form.value.description?.trim() || null,
      questionCount: form.value.questionCount,
      shuffleQuestions: form.value.shuffleQuestions,
      shuffleOptions: form.value.shuffleOptions,
      timeLimitMinutes: form.value.timeLimitMinutes || null
    })
    showCreate.value = false
    form.value = {
      title: '', description: '', questionCount: 10,
      shuffleQuestions: true, shuffleOptions: true, timeLimitMinutes: null
    }
    await load()
  } catch (e) {
    createError.value = e.response?.data?.message || 'Gagal membuat quiz.'
  } finally {
    creating.value = false
  }
}

async function confirmDelete(quiz) {
  if (!confirm(`Hapus practice quiz "${quiz.title}"? Semua data attempt akan terhapus.`)) return
  try {
    await practiceApi.remove(quiz.id)
    await load()
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal menghapus quiz.')
  }
}

function formatDate(dt) {
  if (!dt) return ''
  return new Date(dt).toLocaleDateString('id-ID', { day: 'numeric', month: 'short', year: 'numeric' })
}

onMounted(load)
</script>

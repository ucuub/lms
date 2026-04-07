<template>
  <div class="max-w-3xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-xl font-bold flex-1">Kelola Soal</h1>
      <button @click="showForm = !showForm" class="btn-primary btn-sm">+ Tambah Soal</button>
    </div>

    <!-- Add Question Form -->
    <div v-if="showForm" class="card p-5 mb-4">
      <h3 class="font-medium mb-3">Tambah Soal Baru</h3>
      <div class="space-y-3">
        <div>
          <label class="label">Tipe Soal</label>
          <select v-model="newQ.type" class="select">
            <option value="MultipleChoice">Pilihan Ganda</option>
            <option value="TrueFalse">Benar/Salah</option>
            <option value="Essay">Essay</option>
          </select>
        </div>
        <div>
          <label class="label">Pertanyaan</label>
          <textarea v-model="newQ.text" class="textarea" rows="2" required></textarea>
        </div>
        <div>
          <label class="label">Poin</label>
          <input v-model.number="newQ.points" type="number" class="input w-24" min="1" />
        </div>
        <div v-if="newQ.type === 'MultipleChoice'">
          <label class="label">Opsi Jawaban (pilih jawaban benar)</label>
          <div v-for="(opt, i) in newQ.options" :key="i" class="flex gap-2 mb-2">
            <input type="radio" name="correct" :checked="opt.isCorrect" @change="setCorrect(i)" class="mt-2.5" />
            <input v-model="opt.text" class="input" :placeholder="`Opsi ${i + 1}`" />
            <button type="button" @click="newQ.options.splice(i, 1)" class="text-red-400 hover:text-red-600 text-lg">✕</button>
          </div>
          <button type="button" @click="newQ.options.push({ text: '', isCorrect: false })" class="btn-outline btn-sm">+ Opsi</button>
        </div>
        <div v-if="newQ.type === 'TrueFalse'">
          <label class="label">Jawaban Benar</label>
          <select v-model="newQ.tfAnswer" class="select">
            <option value="true">Benar (True)</option>
            <option value="false">Salah (False)</option>
          </select>
        </div>
        <div class="flex gap-2">
          <button @click="addQuestion" :disabled="saving" class="btn-primary btn-sm">
            {{ saving ? 'Menyimpan...' : 'Simpan Soal' }}
          </button>
          <button @click="showForm = false" class="btn-outline btn-sm">Batal</button>
        </div>
      </div>
    </div>

    <!-- Questions List -->
    <div class="space-y-3">
      <div v-for="(q, idx) in questions" :key="q.id" class="card p-4">
        <div class="flex items-start justify-between gap-3">
          <div class="flex-1">
            <div class="flex items-center gap-2 mb-1.5">
              <span class="badge-gray badge">{{ idx + 1 }}</span>
              <span class="badge-blue badge text-xs">{{ q.type }}</span>
              <span class="text-xs text-gray-500">{{ q.points }} poin</span>
            </div>
            <p class="text-sm text-gray-800 font-medium">{{ q.text }}</p>
            <div v-if="q.options?.length" class="mt-2 space-y-1">
              <div v-for="opt in q.options" :key="opt.id"
                :class="['text-xs px-2 py-1 rounded', opt.isCorrect ? 'bg-green-100 text-green-700 font-semibold' : 'text-gray-500']">
                {{ opt.isCorrect ? '✓ ' : '○ ' }}{{ opt.text }}
              </div>
            </div>
          </div>
          <button @click="deleteQ(q.id)" class="text-red-400 hover:text-red-600 text-sm shrink-0 mt-1">Hapus</button>
        </div>
      </div>
      <div v-if="questions.length === 0" class="card p-12 text-center text-gray-400">
        Belum ada soal. Tambahkan soal di atas.
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { quizzesApi } from '@/api/quizzes'

const route = useRoute()
const questions = ref([])
const showForm = ref(false)
const saving = ref(false)

const newQ = reactive({
  text: '',
  type: 'MultipleChoice',
  points: 10,
  options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }],
  tfAnswer: 'true'
})

function setCorrect(idx) {
  newQ.options.forEach((o, i) => { o.isCorrect = i === idx })
}

async function load() {
  const { data } = await quizzesApi.getQuestions(route.params.id)
  questions.value = data
}

async function addQuestion() {
  if (!newQ.text.trim()) return
  saving.value = true
  try {
    let opts = []
    if (newQ.type === 'MultipleChoice') opts = newQ.options.filter(o => o.text.trim())
    if (newQ.type === 'TrueFalse') {
      opts = [
        { text: 'Benar', isCorrect: newQ.tfAnswer === 'true' },
        { text: 'Salah', isCorrect: newQ.tfAnswer === 'false' }
      ]
    }
    await quizzesApi.addQuestion(route.params.id, {
      text: newQ.text, type: newQ.type, points: newQ.points, options: opts
    })
    showForm.value = false
    newQ.text = ''
    newQ.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
    load()
  } finally {
    saving.value = false
  }
}

async function deleteQ(id) {
  if (!confirm('Hapus soal ini?')) return
  await quizzesApi.deleteQuestion(id)
  load()
}

onMounted(load)
</script>

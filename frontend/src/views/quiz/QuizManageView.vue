<template>
  <div class="max-w-3xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-xl font-bold flex-1">Kelola Soal</h1>
      <button @click="openBank" class="btn-outline btn-sm">📚 Import dari Bank</button>
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

  <!-- Modal Import dari Bank Soal -->
  <div v-if="showBank" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4" @click.self="showBank = false">
    <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl flex flex-col max-h-[80vh]">
      <!-- Header -->
      <div class="flex items-center justify-between px-5 py-4 border-b border-gray-100">
        <h3 class="font-semibold text-gray-900">Import dari Bank Soal</h3>
        <button @click="showBank = false" class="text-gray-400 hover:text-gray-600">✕</button>
      </div>

      <!-- Filter -->
      <div class="px-5 py-3 border-b border-gray-100">
        <input v-model="bankFilter" @input="loadBank" class="input max-w-xs" placeholder="Filter kategori..." />
      </div>

      <!-- List soal -->
      <div class="flex-1 overflow-y-auto px-5 py-3 space-y-2">
        <div v-if="bankLoading" class="flex justify-center py-8">
          <div class="animate-spin rounded-full h-6 w-6 border-2 border-blue-600 border-t-transparent"></div>
        </div>
        <div v-else-if="bankItems.length === 0" class="text-center py-8 text-gray-400">
          Bank soal kosong.
        </div>
        <label v-for="q in bankItems" :key="q.id"
          class="flex items-start gap-3 p-3 rounded-lg border border-gray-100 hover:bg-blue-50 cursor-pointer transition"
          :class="selectedIds.includes(q.id) ? 'border-blue-400 bg-blue-50' : ''">
          <input type="checkbox" :value="q.id" v-model="selectedIds" class="mt-0.5 shrink-0" />
          <div class="flex-1 min-w-0">
            <div class="flex gap-2 mb-1 flex-wrap">
              <span v-if="q.category" class="badge-blue badge text-xs">{{ q.category }}</span>
              <span class="badge-gray badge text-xs">{{ q.type }}</span>
              <span class="text-xs text-gray-400">{{ q.points }} poin</span>
            </div>
            <p class="text-sm text-gray-800">{{ q.text }}</p>
            <div v-if="q.options?.length" class="flex flex-wrap gap-1 mt-1">
              <span v-for="o in q.options" :key="o.id"
                :class="['text-xs px-2 py-0.5 rounded', o.isCorrect ? 'bg-green-100 text-green-700 font-medium' : 'bg-gray-100 text-gray-500']">
                {{ o.text }}
              </span>
            </div>
          </div>
        </label>
      </div>

      <!-- Footer -->
      <div class="flex items-center justify-between px-5 py-4 border-t border-gray-100 bg-gray-50 rounded-b-xl">
        <span class="text-sm text-gray-500">{{ selectedIds.length }} soal dipilih</span>
        <div class="flex gap-2">
          <button @click="showBank = false" class="btn-outline btn-sm">Batal</button>
          <button @click="doImport" :disabled="selectedIds.length === 0 || importing" class="btn-primary btn-sm">
            {{ importing ? 'Mengimpor...' : `Import ${selectedIds.length} Soal` }}
          </button>
        </div>
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

// Bank soal
const showBank = ref(false)
const bankItems = ref([])
const bankFilter = ref('')
const bankLoading = ref(false)
const selectedIds = ref([])
const importing = ref(false)

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

async function openBank() {
  showBank.value = true
  selectedIds.value = []
  await loadBank()
}

async function loadBank() {
  bankLoading.value = true
  try {
    const params = { pageSize: 50 }
    if (bankFilter.value) params.category = bankFilter.value
    const { data } = await quizzesApi.getBank(params)
    bankItems.value = data.items
  } finally {
    bankLoading.value = false
  }
}

async function doImport() {
  if (!selectedIds.value.length) return
  importing.value = true
  try {
    await quizzesApi.importFromBank(route.params.id, selectedIds.value)
    showBank.value = false
    selectedIds.value = []
    load()
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal import soal.')
  } finally {
    importing.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="p-6 max-w-4xl mx-auto">
    <div class="flex items-center gap-3 mb-6">
      <RouterLink to="/question-sets" class="text-gray-400 hover:text-gray-600">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
        </svg>
      </RouterLink>
      <h1 class="text-2xl font-bold text-gray-900">{{ isEdit ? 'Edit Paket Soal' : 'Buat Paket Soal' }}</h1>
    </div>

    <!-- Form info dasar -->
    <div class="bg-white rounded-xl border border-gray-200 p-5 mb-6">
      <h2 class="font-semibold text-gray-800 mb-4">Informasi Dasar</h2>
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Judul <span class="text-red-500">*</span></label>
          <input v-model="form.title" type="text" placeholder="Contoh: Latihan Matematika Kelas 10"
            class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-1">Deskripsi</label>
          <textarea v-model="form.description" rows="2" placeholder="Deskripsi paket soal (opsional)"
            class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none" />
        </div>
        <div class="grid grid-cols-3 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Batas Waktu (menit)</label>
            <input v-model.number="form.timeLimitMinutes" type="number" min="1" placeholder="Tanpa batas"
              class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Maks. Percobaan</label>
            <input v-model.number="form.maxAttempts" type="number" min="1"
              class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Nilai Lulus (%)</label>
            <input v-model.number="form.passScore" type="number" min="1" max="100"
              class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>
        <div class="flex items-center gap-2">
          <input v-model="form.isPublished" type="checkbox" id="published" class="rounded text-blue-600" />
          <label for="published" class="text-sm text-gray-700">Publish (siswa bisa melihat & mengerjakan)</label>
        </div>
      </div>
      <div class="flex justify-end mt-4">
        <button @click="saveInfo" :disabled="saving"
          class="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors">
          {{ isEdit ? 'Simpan Perubahan' : 'Simpan & Lanjut' }}
        </button>
      </div>
    </div>

    <!-- Soal-soal (hanya tampil setelah berhasil disimpan) -->
    <div v-if="setId" class="bg-white rounded-xl border border-gray-200 p-5">
      <div class="flex items-center justify-between mb-4">
        <h2 class="font-semibold text-gray-800">
          Daftar Soal
          <span class="ml-2 text-sm font-normal text-gray-400">({{ questions.length }} soal)</span>
        </h2>
        <div class="flex gap-2">
          <button @click="showBankModal = true"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-blue-600 border border-blue-200 rounded-lg hover:bg-blue-50 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
            </svg>
            Import dari Bank Soal
          </button>
          <button @click="openAddQuestion"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 transition-colors">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            Tambah Soal Custom
          </button>
        </div>
      </div>

      <!-- Question list -->
      <div v-if="questions.length === 0" class="text-center py-8 text-gray-400 text-sm">
        Belum ada soal. Tambahkan soal atau import dari bank soal.
      </div>
      <div v-else class="space-y-3">
        <div v-for="(q, idx) in questions" :key="q.id"
          class="flex items-start gap-3 p-4 border border-gray-100 rounded-lg bg-gray-50">
          <span class="w-7 h-7 flex items-center justify-center bg-blue-100 text-blue-700 text-sm font-bold rounded-full shrink-0">
            {{ idx + 1 }}
          </span>
          <div class="flex-1 min-w-0">
            <p class="text-sm text-gray-800">{{ q.text }}</p>
            <div class="flex items-center gap-3 mt-1">
              <span class="text-xs text-gray-400">{{ typeLabel(q.type) }}</span>
              <span class="text-xs text-gray-400">{{ q.points }} poin</span>
              <span v-if="q.bankQuestionId" class="text-xs bg-purple-100 text-purple-600 px-1.5 py-0.5 rounded">
                Dari Bank
              </span>
            </div>
            <div v-if="q.options?.length" class="mt-2 space-y-1">
              <div v-for="opt in q.options" :key="opt.id"
                :class="opt.isCorrect ? 'text-green-700 font-medium' : 'text-gray-500'"
                class="text-xs flex items-center gap-1">
                <span :class="opt.isCorrect ? 'text-green-500' : 'text-gray-300'">
                  {{ opt.isCorrect ? '✓' : '○' }}
                </span>
                {{ opt.text }}
              </div>
            </div>
          </div>
          <div class="flex gap-1 shrink-0">
            <button @click="editQuestion(q)" class="p-1.5 text-gray-400 hover:text-blue-600 transition-colors">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/>
              </svg>
            </button>
            <button @click="removeQuestion(q.id)" class="p-1.5 text-gray-400 hover:text-red-500 transition-colors">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: Import dari Bank Soal -->
    <div v-if="showBankModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl max-h-[80vh] flex flex-col">
        <div class="flex items-center justify-between p-5 border-b">
          <h3 class="font-semibold text-gray-900">Import dari Bank Soal</h3>
          <button @click="showBankModal = false" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div class="p-4 border-b flex gap-3">
          <input v-model="bankSearch" placeholder="Cari soal..." type="text"
            class="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
          <select v-model="bankCategory" class="border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="">Semua Kategori</option>
            <option v-for="cat in bankCategories" :key="cat" :value="cat">{{ cat }}</option>
          </select>
        </div>

        <div class="flex-1 overflow-y-auto p-4 space-y-2">
          <div v-if="loadingBank" class="text-center py-6 text-gray-400">Memuat...</div>
          <div v-else-if="filteredBank.length === 0" class="text-center py-6 text-gray-400">
            Tidak ada soal ditemukan.
          </div>
          <label v-for="bq in filteredBank" :key="bq.id"
            class="flex items-start gap-3 p-3 border rounded-lg cursor-pointer hover:bg-gray-50 transition-colors"
            :class="selectedBankIds.includes(bq.id) ? 'border-blue-400 bg-blue-50' : 'border-gray-200'">
            <input type="checkbox" :value="bq.id" v-model="selectedBankIds" class="mt-0.5 rounded text-blue-600" />
            <div class="flex-1 min-w-0">
              <p class="text-sm text-gray-800">{{ bq.text }}</p>
              <div class="flex items-center gap-2 mt-1">
                <span class="text-xs text-gray-400">{{ typeLabel(bq.type) }}</span>
                <span v-if="bq.category" class="text-xs bg-gray-100 text-gray-500 px-1.5 py-0.5 rounded">{{ bq.category }}</span>
                <span class="text-xs text-gray-400">{{ bq.points }} poin</span>
              </div>
            </div>
          </label>
        </div>

        <div class="p-4 border-t flex items-center justify-between">
          <span class="text-sm text-gray-500">{{ selectedBankIds.length }} soal dipilih</span>
          <div class="flex gap-2">
            <button @click="showBankModal = false"
              class="px-4 py-2 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              Batal
            </button>
            <button @click="doImport" :disabled="selectedBankIds.length === 0 || importing"
              class="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors">
              {{ importing ? 'Mengimport...' : 'Import Soal' }}
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal: Tambah/Edit Soal Custom -->
    <div v-if="showQuestionModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-lg max-h-[90vh] flex flex-col">
        <div class="flex items-center justify-between p-5 border-b">
          <h3 class="font-semibold text-gray-900">{{ editingQuestion ? 'Edit Soal' : 'Tambah Soal' }}</h3>
          <button @click="showQuestionModal = false" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div class="flex-1 overflow-y-auto p-5 space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Pertanyaan <span class="text-red-500">*</span></label>
            <textarea v-model="qForm.text" rows="3" placeholder="Tulis pertanyaan di sini..."
              class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none" />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Tipe</label>
              <select v-model="qForm.type" @change="onTypeChange"
                class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500">
                <option value="MultipleChoice">Pilihan Ganda</option>
                <option value="TrueFalse">Benar/Salah</option>
                <option value="Essay">Essay</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Poin</label>
              <input v-model.number="qForm.points" type="number" min="1"
                class="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
            </div>
          </div>

          <!-- Pilihan jawaban (untuk MultipleChoice & TrueFalse) -->
          <div v-if="qForm.type !== 'Essay'">
            <div class="flex items-center justify-between mb-2">
              <label class="text-sm font-medium text-gray-700">Pilihan Jawaban</label>
              <button v-if="qForm.type === 'MultipleChoice'" @click="addOption"
                class="text-xs text-blue-600 hover:underline">+ Tambah Pilihan</button>
            </div>
            <div class="space-y-2">
              <div v-for="(opt, i) in qForm.options" :key="i" class="flex items-center gap-2">
                <input type="radio" :name="`correct-${setId}`" :value="i" v-model="qForm.correctIndex"
                  class="text-blue-600" />
                <input v-model="opt.text" type="text" placeholder="Teks pilihan..."
                  class="flex-1 border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
                <button v-if="qForm.type === 'MultipleChoice' && qForm.options.length > 2"
                  @click="qForm.options.splice(i, 1); if(qForm.correctIndex >= qForm.options.length) qForm.correctIndex = 0"
                  class="text-gray-300 hover:text-red-500 transition-colors">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                  </svg>
                </button>
              </div>
            </div>
          </div>
        </div>

        <div class="p-4 border-t flex justify-end gap-2">
          <button @click="showQuestionModal = false"
            class="px-4 py-2 text-sm text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            Batal
          </button>
          <button @click="saveQuestion" :disabled="savingQ"
            class="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 transition-colors">
            {{ savingQ ? 'Menyimpan...' : 'Simpan Soal' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { questionSetsApi } from '@/api/questionSets'
import { quizzesApi } from '@/api/quizzes'

const route = useRoute()
const router = useRouter()

const setId = ref(route.params.id ? Number(route.params.id) : null)
const isEdit = computed(() => !!setId.value)

const form = ref({ title: '', description: '', timeLimitMinutes: null, maxAttempts: 1, passScore: 60, isPublished: false })
const questions = ref([])
const saving = ref(false)

// Bank modal
const showBankModal = ref(false)
const bankQuestions = ref([])
const loadingBank = ref(false)
const bankSearch = ref('')
const bankCategory = ref('')
const selectedBankIds = ref([])
const importing = ref(false)

const bankCategories = computed(() => [...new Set(bankQuestions.value.map(q => q.category).filter(Boolean))])
const filteredBank = computed(() => bankQuestions.value.filter(q => {
  const matchSearch = !bankSearch.value || q.text.toLowerCase().includes(bankSearch.value.toLowerCase())
  const matchCat = !bankCategory.value || q.category === bankCategory.value
  return matchSearch && matchCat
}))

// Question modal
const showQuestionModal = ref(false)
const editingQuestion = ref(null)
const savingQ = ref(false)
const qForm = ref({ text: '', type: 'MultipleChoice', points: 10, options: [], correctIndex: 0 })

onMounted(async () => {
  if (isEdit.value) {
    const { data } = await questionSetsApi.getById(setId.value)
    form.value = {
      title: data.title,
      description: data.description ?? '',
      timeLimitMinutes: data.timeLimitMinutes,
      maxAttempts: data.maxAttempts,
      passScore: data.passScore,
      isPublished: data.isPublished
    }
    questions.value = data.questions ?? []
  }
})

watch(showBankModal, async (val) => {
  if (val && bankQuestions.value.length === 0) {
    loadingBank.value = true
    try {
      const { data } = await quizzesApi.getBank()
      bankQuestions.value = data
    } finally {
      loadingBank.value = false
    }
  }
})

async function saveInfo() {
  if (!form.value.title.trim()) return alert('Judul wajib diisi.')
  saving.value = true
  try {
    const payload = { ...form.value, timeLimitMinutes: form.value.timeLimitMinutes || null }
    if (isEdit.value) {
      await questionSetsApi.update(setId.value, payload)
    } else {
      const { data } = await questionSetsApi.create(payload)
      setId.value = data.id
      router.replace(`/question-sets/${data.id}/manage`)
    }
  } finally {
    saving.value = false
  }
}

async function doImport() {
  if (!selectedBankIds.value.length) return
  importing.value = true
  try {
    const { data } = await questionSetsApi.importFromBank(setId.value, selectedBankIds.value)
    questions.value.push(...data)
    selectedBankIds.value = []
    showBankModal.value = false
  } finally {
    importing.value = false
  }
}

function openAddQuestion() {
  editingQuestion.value = null
  qForm.value = { text: '', type: 'MultipleChoice', points: 10, correctIndex: 0, options: [
    { text: '' }, { text: '' }, { text: '' }, { text: '' }
  ]}
  showQuestionModal.value = true
}

function editQuestion(q) {
  editingQuestion.value = q
  const correctIndex = q.options?.findIndex(o => o.isCorrect) ?? 0
  qForm.value = {
    text: q.text,
    type: q.type,
    points: q.points,
    correctIndex: correctIndex < 0 ? 0 : correctIndex,
    options: q.options?.map(o => ({ text: o.text, isCorrect: o.isCorrect })) ?? []
  }
  showQuestionModal.value = true
}

function onTypeChange() {
  if (qForm.value.type === 'TrueFalse') {
    qForm.value.options = [{ text: 'Benar' }, { text: 'Salah' }]
    qForm.value.correctIndex = 0
  } else if (qForm.value.type === 'MultipleChoice') {
    qForm.value.options = [{ text: '' }, { text: '' }, { text: '' }, { text: '' }]
    qForm.value.correctIndex = 0
  } else {
    qForm.value.options = []
  }
}

function addOption() {
  qForm.value.options.push({ text: '' })
}

async function saveQuestion() {
  if (!qForm.value.text.trim()) return alert('Pertanyaan wajib diisi.')
  savingQ.value = true
  try {
    const opts = qForm.value.options.map((o, i) => ({ text: o.text, isCorrect: i === qForm.value.correctIndex }))
    const payload = {
      text: qForm.value.text,
      type: qForm.value.type,
      points: qForm.value.points,
      order: editingQuestion.value?.order ?? questions.value.length,
      options: qForm.value.type === 'Essay' ? [] : opts
    }
    if (editingQuestion.value) {
      const { data } = await questionSetsApi.updateQuestion(editingQuestion.value.id, payload)
      const idx = questions.value.findIndex(q => q.id === editingQuestion.value.id)
      if (idx !== -1) questions.value[idx] = data
    } else {
      const { data } = await questionSetsApi.addQuestion(setId.value, payload)
      questions.value.push(data)
    }
    showQuestionModal.value = false
  } finally {
    savingQ.value = false
  }
}

async function removeQuestion(id) {
  if (!confirm('Hapus soal ini?')) return
  await questionSetsApi.deleteQuestion(id)
  questions.value = questions.value.filter(q => q.id !== id)
}

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}
</script>

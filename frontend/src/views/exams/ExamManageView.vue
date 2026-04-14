<template>
  <div class="max-w-3xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-xl font-bold flex-1">{{ isEdit ? 'Kelola Ujian' : 'Buat Ujian Baru' }}</h1>
    </div>

    <!-- Form Info Ujian -->
    <div class="card p-5 mb-5">
      <h2 class="font-semibold text-gray-900 mb-4">Informasi Ujian</h2>
      <form @submit.prevent="saveExam" class="space-y-4">
        <div>
          <label class="label">Judul Ujian</label>
          <input v-model="form.title" class="input" required />
        </div>
        <div>
          <label class="label">Deskripsi (opsional)</label>
          <textarea v-model="form.description" class="textarea" rows="2"></textarea>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="label">Batas Waktu (menit)</label>
            <input v-model.number="form.timeLimitMinutes" type="number" class="input" min="1" />
          </div>
          <div>
            <label class="label">Nilai Lulus (%)</label>
            <input v-model.number="form.passScore" type="number" class="input" min="0" max="100" />
          </div>
        </div>
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="label">Maks Percobaan</label>
            <input v-model.number="form.maxAttempts" type="number" class="input" min="1" />
          </div>
          <div class="flex items-end pb-1">
            <label class="flex items-center gap-2 cursor-pointer">
              <input v-model="form.isPublished" type="checkbox" class="w-4 h-4" />
              <span class="text-sm">Publikasikan ujian</span>
            </label>
          </div>
        </div>
        <div class="flex gap-3">
          <button type="submit" :disabled="saving" class="btn-primary">
            {{ saving ? 'Menyimpan...' : (isEdit ? 'Simpan Perubahan' : 'Buat Ujian') }}
          </button>
        </div>
      </form>
    </div>

    <!-- Manajemen Soal — hanya muncul setelah ujian dibuat -->
    <div v-if="isEdit" class="card p-5">
      <div class="flex items-center justify-between mb-4">
        <h2 class="font-semibold text-gray-900">Daftar Soal ({{ questions.length }})</h2>
        <button @click="showForm = !showForm" class="btn-primary btn-sm">+ Tambah Soal</button>
      </div>

      <!-- Form Tambah / Edit Soal -->
      <div v-if="showForm" class="bg-gray-50 rounded-lg p-4 mb-4 border border-gray-200">
        <h3 class="font-medium text-gray-800 mb-3">{{ editingQ ? 'Edit Soal' : 'Soal Baru' }}</h3>
        <div class="space-y-3">
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Tipe Soal</label>
              <select v-model="qForm.type" class="select" :disabled="!!editingQ">
                <option value="MultipleChoice">Pilihan Ganda</option>
                <option value="TrueFalse">Benar/Salah</option>
                <option value="Essay">Essay</option>
              </select>
            </div>
            <div>
              <label class="label">Poin</label>
              <input v-model.number="qForm.points" type="number" class="input" min="1" />
            </div>
          </div>
          <div>
            <label class="label">Pertanyaan</label>
            <textarea v-model="qForm.text" class="textarea" rows="2" required></textarea>
          </div>

          <!-- Pilihan Ganda -->
          <div v-if="qForm.type === 'MultipleChoice'">
            <label class="label">Opsi Jawaban (pilih jawaban benar)</label>
            <div v-for="(opt, i) in qForm.options" :key="i" class="flex gap-2 mb-2">
              <input type="radio" name="qCorrect" :checked="opt.isCorrect"
                @change="setCorrect(i)" class="mt-2.5 shrink-0" />
              <input v-model="opt.text" class="input" :placeholder="`Opsi ${i + 1}`" />
              <button type="button" @click="qForm.options.splice(i, 1)"
                class="text-red-400 hover:text-red-600 text-lg shrink-0">✕</button>
            </div>
            <button type="button" @click="qForm.options.push({ text: '', isCorrect: false })"
              class="btn-outline btn-sm">+ Opsi</button>
          </div>

          <!-- Benar/Salah -->
          <div v-if="qForm.type === 'TrueFalse'">
            <label class="label">Jawaban Benar</label>
            <select v-model="qForm.tfAnswer" class="select">
              <option value="true">Benar (True)</option>
              <option value="false">Salah (False)</option>
            </select>
          </div>

          <div class="flex gap-2">
            <button @click="saveQuestion" :disabled="savingQ" class="btn-primary btn-sm">
              {{ savingQ ? 'Menyimpan...' : (editingQ ? 'Update Soal' : 'Simpan Soal') }}
            </button>
            <button @click="cancelQForm" class="btn-outline btn-sm">Batal</button>
          </div>
        </div>
      </div>

      <!-- List Soal -->
      <div class="space-y-3">
        <div v-for="(q, idx) in questions" :key="q.id" class="border border-gray-100 rounded-lg p-4">
          <div class="flex items-start justify-between gap-3">
            <div class="flex-1">
              <div class="flex items-center gap-2 mb-1.5 flex-wrap">
                <span class="badge-gray badge">{{ idx + 1 }}</span>
                <span class="badge-blue badge text-xs">{{ typeLabel(q.type) }}</span>
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
            <div class="flex gap-2 shrink-0">
              <button @click="openEdit(q)" class="text-blue-500 hover:text-blue-700 text-sm">Edit</button>
              <button @click="deleteQ(q.id)" class="text-red-400 hover:text-red-600 text-sm">Hapus</button>
            </div>
          </div>
        </div>

        <div v-if="questions.length === 0" class="text-center py-8 text-gray-400 text-sm">
          Belum ada soal. Klik "+ Tambah Soal" untuk mulai.
        </div>
      </div>

      <!-- Summary -->
      <div v-if="questions.length > 0" class="mt-4 p-3 bg-blue-50 rounded-lg text-sm text-blue-700">
        Total: <strong>{{ questions.length }} soal</strong> ·
        <strong>{{ totalPoints }} poin</strong>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { examsApi } from '@/api/exams'

const route = useRoute()
const router = useRouter()
const isEdit = computed(() => !!route.params.id && route.params.id !== 'create')

const saving = ref(false)
const savingQ = ref(false)
const showForm = ref(false)
const editingQ = ref(null)
const questions = ref([])

const form = reactive({
  title: '',
  description: '',
  timeLimitMinutes: 60,
  maxAttempts: 1,
  passScore: 60,
  isPublished: false
})

const qForm = reactive({
  text: '',
  type: 'MultipleChoice',
  points: 10,
  options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }],
  tfAnswer: 'true'
})

const totalPoints = computed(() => questions.value.reduce((s, q) => s + q.points, 0))

function typeLabel(type) {
  return { MultipleChoice: 'Pilihan Ganda', TrueFalse: 'Benar/Salah', Essay: 'Essay' }[type] ?? type
}

function setCorrect(idx) {
  qForm.options.forEach((o, i) => { o.isCorrect = i === idx })
}

function resetQForm() {
  qForm.text = ''
  qForm.type = 'MultipleChoice'
  qForm.points = 10
  qForm.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
  qForm.tfAnswer = 'true'
  editingQ.value = null
}

function cancelQForm() {
  showForm.value = false
  resetQForm()
}

function openEdit(q) {
  editingQ.value = q
  qForm.text = q.text
  qForm.type = q.type
  qForm.points = q.points
  if (q.type === 'MultipleChoice') {
    qForm.options = q.options.map(o => ({ ...o }))
  } else if (q.type === 'TrueFalse') {
    const correct = q.options.find(o => o.isCorrect)
    qForm.tfAnswer = correct?.text === 'Benar' ? 'true' : 'false'
  }
  showForm.value = true
}

async function saveExam() {
  saving.value = true
  try {
    if (isEdit.value) {
      await examsApi.update(route.params.id, { ...form })
      alert('Ujian berhasil diperbarui.')
    } else {
      const { data } = await examsApi.create({ ...form })
      router.replace(`/exams/${data.id}/manage`)
    }
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal menyimpan ujian.')
  } finally {
    saving.value = false
  }
}

async function saveQuestion() {
  if (!qForm.text.trim()) return
  savingQ.value = true
  try {
    let opts = []
    if (qForm.type === 'MultipleChoice') opts = qForm.options.filter(o => o.text.trim())
    if (qForm.type === 'TrueFalse') {
      opts = [
        { text: 'Benar', isCorrect: qForm.tfAnswer === 'true' },
        { text: 'Salah', isCorrect: qForm.tfAnswer === 'false' }
      ]
    }

    const payload = { text: qForm.text, type: qForm.type, points: qForm.points, options: opts }

    if (editingQ.value) {
      const { data } = await examsApi.updateQuestion(editingQ.value.id, payload)
      const idx = questions.value.findIndex(q => q.id === editingQ.value.id)
      if (idx !== -1) questions.value[idx] = data
    } else {
      const order = questions.value.length
      const { data } = await examsApi.addQuestion(route.params.id, { ...payload, order })
      questions.value.push(data)
    }

    cancelQForm()
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal menyimpan soal.')
  } finally {
    savingQ.value = false
  }
}

async function deleteQ(id) {
  if (!confirm('Hapus soal ini?')) return
  await examsApi.deleteQuestion(id)
  questions.value = questions.value.filter(q => q.id !== id)
}

onMounted(async () => {
  if (isEdit.value) {
    const { data } = await examsApi.getById(route.params.id)
    form.title = data.title
    form.description = data.description ?? ''
    form.timeLimitMinutes = data.timeLimitMinutes
    form.maxAttempts = data.maxAttempts
    form.passScore = data.passScore
    form.isPublished = data.isPublished
    questions.value = data.questions ?? []
  }
})
</script>

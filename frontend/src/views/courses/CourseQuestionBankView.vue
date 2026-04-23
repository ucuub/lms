<template>
  <div class="max-w-5xl mx-auto p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <RouterLink :to="`/courses/${courseId}`" class="text-sm text-blue-600 hover:underline">
          ← Kembali ke Kursus
        </RouterLink>
        <h1 class="text-2xl font-bold mt-1">Bank Soal Kursus</h1>
        <p v-if="course" class="text-gray-500 text-sm">{{ course.title }}</p>
      </div>
      <div class="flex gap-2">
        <button v-if="isAdmin" @click="showAiModal = true"
          class="inline-flex items-center gap-1.5 px-4 py-2 bg-purple-600 text-white rounded-lg text-sm font-medium hover:bg-purple-700 transition-colors">
          ✨ Generate AI
        </button>
        <button @click="openAddForm" class="btn-primary">+ Tambah Soal</button>
      </div>
    </div>

    <!-- Filter by module -->
    <div class="flex items-center gap-3 flex-wrap">
      <span class="text-sm text-gray-600 font-medium">Filter Modul:</span>
      <button
        @click="filterModuleId = null"
        :class="filterModuleId === null ? 'btn-primary text-sm py-1 px-3' : 'btn-outline text-sm py-1 px-3'"
      >Semua</button>
      <button
        @click="filterModuleId = 0"
        :class="filterModuleId === 0 ? 'btn-primary text-sm py-1 px-3' : 'btn-outline text-sm py-1 px-3'"
      >Umum (tanpa modul)</button>
      <button
        v-for="m in modules"
        :key="m.id"
        @click="filterModuleId = m.id"
        :class="filterModuleId === m.id ? 'btn-primary text-sm py-1 px-3' : 'btn-outline text-sm py-1 px-3'"
      >{{ m.title }}</button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-4 gap-4">
      <div class="card p-4 text-center">
        <div class="text-2xl font-bold text-blue-600">{{ questions.length }}</div>
        <div class="text-sm text-gray-500">Total Soal</div>
      </div>
      <div class="card p-4 text-center">
        <div class="text-2xl font-bold text-green-600">{{ mcCount }}</div>
        <div class="text-sm text-gray-500">Pilihan Ganda</div>
      </div>
      <div class="card p-4 text-center">
        <div class="text-2xl font-bold text-yellow-600">{{ tfCount }}</div>
        <div class="text-sm text-gray-500">Benar/Salah</div>
      </div>
      <div class="card p-4 text-center">
        <div class="text-2xl font-bold text-purple-600">{{ essayCount }}</div>
        <div class="text-sm text-gray-500">Essay</div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-10">
      <div class="animate-spin rounded-full h-8 w-8 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <!-- Load error -->
    <div v-else-if="loadError" class="alert-error">{{ loadError }}</div>

    <!-- Empty -->
    <div v-else-if="filteredQuestions.length === 0" class="card p-10 text-center text-gray-500">
      <div class="text-4xl mb-3">📚</div>
      <p class="font-medium">Belum ada soal</p>
      <p class="text-sm mt-1">Tambahkan soal pertama untuk bank soal kursus ini.</p>
    </div>

    <!-- Question list -->
    <div v-else-if="!loadError" class="space-y-3">
      <div
        v-for="(q, idx) in filteredQuestions"
        :key="q.id"
        class="card p-5"
      >
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-2 flex-wrap">
              <span class="text-xs font-medium text-gray-400">#{{ idx + 1 }}</span>
              <span :class="typeClass(q.type)" class="badge text-xs">{{ typeLabel(q.type) }}</span>
              <span v-if="q.moduleTitle" class="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded">{{ q.moduleTitle }}</span>
              <span v-else class="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded">Umum</span>
              <span class="text-xs text-gray-400">{{ q.points }} poin</span>
            </div>
            <p class="text-gray-800 font-medium">{{ q.text }}</p>

            <!-- MC / TrueFalse options preview -->
            <div v-if="q.type === 'MultipleChoice' || q.type === 'TrueFalse'" class="mt-3 space-y-1">
              <div
                v-for="opt in q.options"
                :key="opt.id"
                :class="opt.isCorrect ? 'text-green-700 font-medium' : 'text-gray-600'"
                class="text-sm flex items-center gap-2"
              >
                <span :class="opt.isCorrect ? 'text-green-500' : 'text-gray-300'">{{ opt.isCorrect ? '✓' : '○' }}</span>
                {{ opt.text }}
              </div>
            </div>

            <!-- Explanation -->
            <div v-if="q.explanation" class="mt-2 text-sm text-blue-700 bg-blue-50 rounded p-2">
              <span class="font-medium">Penjelasan:</span> {{ q.explanation }}
            </div>
          </div>

          <div class="flex gap-2 shrink-0">
            <button @click="openEdit(q)" class="btn-outline text-sm py-1 px-3">Edit</button>
            <button @click="confirmDelete(q)" class="btn-danger text-sm py-1 px-3">Hapus</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Add/Edit Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/40 z-50 flex items-start justify-center pt-10 overflow-y-auto pb-10">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-2xl mx-4 p-6">
        <h2 class="text-xl font-bold mb-4">{{ editingId ? 'Edit Soal' : 'Tambah Soal' }}</h2>

        <div class="space-y-4">
          <!-- Module -->
          <div>
            <label class="label">Modul (opsional)</label>
            <select v-model="form.moduleId" class="input">
              <option :value="null">— Umum (tidak terikat modul) —</option>
              <option v-for="m in modules" :key="m.id" :value="m.id">{{ m.title }}</option>
            </select>
          </div>

          <!-- Tipe soal -->
          <div>
            <label class="label">Tipe Soal</label>
            <select v-model="form.type" @change="onTypeChange" class="input">
              <option value="0">Pilihan Ganda</option>
              <option value="1">Benar/Salah</option>
              <option value="2">Essay</option>
            </select>
          </div>

          <!-- Teks soal -->
          <div>
            <label class="label">Teks Soal <span class="text-red-500">*</span></label>
            <textarea v-model="form.text" class="input min-h-[80px]" placeholder="Masukkan pertanyaan..."></textarea>
          </div>

          <!-- Poin -->
          <div>
            <label class="label">Poin</label>
            <input v-model.number="form.points" type="number" min="1" max="100" class="input w-32" />
          </div>

          <!-- Options (MC & TrueFalse) -->
          <div v-if="Number(form.type) === 0 || Number(form.type) === 1">
            <label class="label">Pilihan Jawaban</label>
            <div class="space-y-2">
              <div v-for="(opt, i) in form.options" :key="opt._key" class="flex items-center gap-2">
                <input
                  type="radio"
                  :name="'correct-' + editingId"
                  :checked="opt.isCorrect"
                  @change="setCorrect(i)"
                  class="mt-1"
                  title="Tandai sebagai jawaban benar"
                />
                <input v-model="opt.text" class="input flex-1" :placeholder="`Pilihan ${i + 1}`"
                  :readonly="Number(form.type) === 1" :class="Number(form.type) === 1 ? 'bg-gray-50' : ''" />
                <button v-if="Number(form.type) === 0" @click="removeOption(i)"
                  class="text-red-500 hover:text-red-700 text-lg leading-none" title="Hapus pilihan">×</button>
              </div>
            </div>
            <button v-if="Number(form.type) === 0" @click="addOption"
              class="text-sm text-blue-600 hover:underline mt-2">+ Tambah Pilihan</button>
            <p class="text-xs text-gray-400 mt-1">Klik radio button untuk menandai jawaban benar.</p>
          </div>

          <!-- Penjelasan -->
          <div>
            <label class="label">Penjelasan Jawaban (opsional)</label>
            <textarea v-model="form.explanation" class="input min-h-[60px]" placeholder="Jelaskan mengapa jawaban ini benar..."></textarea>
          </div>
        </div>

        <div v-if="formError" class="alert-error mt-4">{{ formError }}</div>

        <div class="flex justify-end gap-3 mt-6">
          <button @click="closeForm" class="btn-outline">Batal</button>
          <button @click="submitForm" :disabled="saving" class="btn-primary">
            {{ saving ? 'Menyimpan...' : (editingId ? 'Simpan Perubahan' : 'Tambah Soal') }}
          </button>
        </div>
      </div>
    </div>

    <!-- AI Generate Modal -->
    <AiGenerateModal v-if="showAiModal"
      :model="aiModel"
      :provider-name="aiProviderName"
      :course-id="courseId"
      @close="showAiModal = false"
      @save="onAiSave" />

    <!-- Delete confirm -->
    <div v-if="deleteTarget" class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center">
      <div class="bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6">
        <h3 class="text-lg font-bold mb-2">Hapus Soal?</h3>
        <p class="text-gray-600 text-sm mb-4">Soal ini akan dihapus permanen dari bank soal kursus.</p>
        <div class="bg-gray-50 rounded p-3 text-sm text-gray-700 mb-4 line-clamp-3">{{ deleteTarget.text }}</div>
        <div class="flex justify-end gap-3">
          <button @click="deleteTarget = null" class="btn-outline">Batal</button>
          <button @click="doDelete" :disabled="deleting" class="btn-danger">
            {{ deleting ? 'Menghapus...' : 'Ya, Hapus' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { courseQuestionBankApi } from '@/api/courseQuestionBank'
import { coursesApi, modulesApi } from '@/api/courses'
import { aiApi } from '@/api/ai'
import AiGenerateModal from '@/components/AiGenerateModal.vue'

const route = useRoute()
const courseId = Number(route.params.courseId)

const course     = ref(null)
const modules    = ref([])
const questions  = ref([])
const loading    = ref(true)
const loadError  = ref('')
const filterModuleId = ref(null)  // null = all, 0 = no module, N = specific module

// AI Generate
const showAiModal = ref(false)
const aiModel        = ref('Meta-Llama-3.3-70B-Instruct')
const aiProviderName = ref('DekaLLM')
const isAdmin     = ref(false)

let _optKey = 0
const newOptKey = () => ++_optKey

// Form state
const showForm   = ref(false)
const editingId  = ref(null)
const saving     = ref(false)
const formError  = ref('')
const form = ref(resetForm())

// Delete state
const deleteTarget = ref(null)
const deleting     = ref(false)

// ── Computed ──────────────────────────────────────────────────────────────────

const filteredQuestions = computed(() => {
  if (filterModuleId.value === null) return questions.value
  if (filterModuleId.value === 0)    return questions.value.filter(q => !q.moduleId)
  return questions.value.filter(q => q.moduleId === filterModuleId.value)
})

const mcCount    = computed(() => questions.value.filter(q => q.type === 'MultipleChoice').length)
const tfCount    = computed(() => questions.value.filter(q => q.type === 'TrueFalse').length)
const essayCount = computed(() => questions.value.filter(q => q.type === 'Essay').length)

// ── Lifecycle ─────────────────────────────────────────────────────────────────

onMounted(async () => {
  try {
    const [courseRes, modulesRes, questionsRes] = await Promise.all([
      coursesApi.getById(courseId),
      modulesApi.getAll(courseId),
      courseQuestionBankApi.getAll(courseId),
    ])
    course.value    = courseRes.data
    modules.value   = modulesRes.data ?? []
    questions.value = questionsRes.data ?? []

    // Cek status AI (admin only)
    try {
      const aiStatus = await aiApi.status()
      isAdmin.value  = true
      aiModel.value        = aiStatus.data.model ?? 'Meta-Llama-3.3-70B-Instruct'
      aiProviderName.value = aiStatus.data.providerName ?? 'DekaLLM'
    } catch { /* bukan admin atau AI tidak dikonfigurasi */ }
  } catch (e) {
    loadError.value = e.response?.data?.message ?? 'Gagal memuat data. Coba muat ulang halaman.'
  } finally {
    loading.value = false
  }
})

// ── Methods ───────────────────────────────────────────────────────────────────

function resetForm() {
  return { moduleId: null, type: '0', text: '', points: 10, explanation: '', options: [
    { _key: newOptKey(), text: '', isCorrect: true },
    { _key: newOptKey(), text: '', isCorrect: false },
  ]}
}

function openAddForm() {
  editingId.value = null
  form.value = resetForm()
  formError.value = ''
  showForm.value = true
}

function openEdit(q) {
  editingId.value = q.id
  form.value = {
    moduleId:    q.moduleId ?? null,
    type:        q.type === 'Essay' ? '2' : q.type === 'TrueFalse' ? '1' : '0',
    text:        q.text,
    points:      q.points,
    explanation: q.explanation ?? '',
    options:     q.options.map(o => ({ _key: newOptKey(), text: o.text, isCorrect: o.isCorrect })),
  }
  formError.value = ''
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  editingId.value = null
}

function addOption() {
  form.value.options.push({ _key: newOptKey(), text: '', isCorrect: false })
}

function removeOption(i) {
  if (form.value.options.length <= 2) return
  const wasCorrect = form.value.options[i].isCorrect
  form.value.options.splice(i, 1)
  if (wasCorrect && form.value.options.length > 0)
    form.value.options[0].isCorrect = true
}

function setCorrect(i) {
  form.value.options.forEach((o, idx) => { o.isCorrect = idx === i })
}

async function submitForm() {
  formError.value = ''
  const typeNum = Number(form.value.type)

  if (!form.value.text.trim()) {
    formError.value = 'Teks soal tidak boleh kosong.'
    return
  }
  if (typeNum === 0) {
    if (form.value.options.some(o => !o.text.trim())) {
      formError.value = 'Semua pilihan harus diisi.'
      return
    }
    if (!form.value.options.some(o => o.isCorrect)) {
      formError.value = 'Pilih jawaban yang benar.'
      return
    }
  }

  saving.value = true
  try {
    const payload = {
      moduleId:    form.value.moduleId,
      text:        form.value.text.trim(),
      type:        typeNum,
      points:      form.value.points,
      explanation: form.value.explanation?.trim() || null,
      options:     (typeNum === 0 || typeNum === 1) ? form.value.options : [],
    }

    if (editingId.value) {
      const res = await courseQuestionBankApi.update(courseId, editingId.value, payload)
      const idx = questions.value.findIndex(q => q.id === editingId.value)
      if (idx !== -1) questions.value[idx] = res.data
    } else {
      const res = await courseQuestionBankApi.create(courseId, payload)
      questions.value.unshift(res.data)
    }
    closeForm()
  } catch (e) {
    formError.value = e.response?.data?.message ?? 'Terjadi kesalahan. Coba lagi.'
  } finally {
    saving.value = false
  }
}

function confirmDelete(q) {
  deleteTarget.value = q
}

async function doDelete() {
  if (!deleteTarget.value) return
  deleting.value = true
  try {
    await courseQuestionBankApi.delete(courseId, deleteTarget.value.id)
    questions.value = questions.value.filter(q => q.id !== deleteTarget.value.id)
    deleteTarget.value = null
  } catch (e) {
    alert(e.response?.data?.message ?? 'Gagal menghapus soal.')
  } finally {
    deleting.value = false
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

async function onAiSave(generatedQuestions) {
  for (const q of generatedQuestions) {
    try {
      const typeMap = { MultipleChoice: 0, TrueFalse: 1, Essay: 2 }
      const payload = {
        moduleId:    null,
        text:        q.text,
        type:        typeMap[q.type] ?? 0,
        points:      q.points,
        explanation: q.explanation || null,
        options:     (q.options ?? []).map(o => ({ text: o.text, isCorrect: o.isCorrect })),
      }
      const res = await courseQuestionBankApi.create(courseId, payload)
      questions.value.unshift(res.data)
    } catch (e) {
      console.error('Gagal simpan soal AI:', e)
    }
  }
  showAiModal.value = false
}

function onTypeChange() {
  const t = Number(form.value.type)
  if (t === 1) {
    form.value.options = [
      { _key: newOptKey(), text: 'Benar', isCorrect: true },
      { _key: newOptKey(), text: 'Salah', isCorrect: false },
    ]
  } else if (t === 0) {
    form.value.options = [
      { _key: newOptKey(), text: '', isCorrect: true },
      { _key: newOptKey(), text: '', isCorrect: false },
    ]
  } else {
    form.value.options = []
  }
}

function typeLabel(type) {
  if (type === 'MultipleChoice') return 'Pilihan Ganda'
  if (type === 'TrueFalse') return 'Benar/Salah'
  return 'Essay'
}

function typeClass(type) {
  if (type === 'MultipleChoice') return 'bg-blue-100 text-blue-700'
  if (type === 'TrueFalse') return 'bg-yellow-100 text-yellow-700'
  return 'bg-purple-100 text-purple-700'
}
</script>

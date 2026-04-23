<template>
  <div class="fixed inset-0 z-[70] flex items-center justify-center bg-black/50 p-4">
    <div class="bg-white rounded-2xl shadow-xl w-full max-w-2xl flex flex-col max-h-[90vh]">

      <!-- Header -->
      <div class="flex items-center justify-between p-5 border-b shrink-0">
        <div class="flex items-center gap-2">
          <span class="text-xl">✨</span>
          <div>
            <h3 class="font-semibold text-gray-900">Generate Soal dengan AI</h3>
            <p class="text-xs text-gray-400 mt-0.5">Powered by {{ providerName }} · {{ model }}</p>
            <p v-if="courseId" class="text-xs text-green-600 mt-0.5">✓ Menggunakan materi kursus sebagai referensi</p>
          </div>
        </div>
        <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
          </svg>
        </button>
      </div>

      <!-- Form / Result -->
      <div class="flex-1 overflow-y-auto p-5">

        <!-- Step 1: Form -->
        <div v-if="step === 'form'" class="space-y-4">
          <!-- Topik -->
          <div>
            <label class="label">Topik Soal <span class="text-red-500">*</span></label>
            <textarea v-model="form.topic" rows="2" class="input"
              placeholder="Contoh: Fotosintesis pada tumbuhan, Hukum Newton, Revolusi Industri..." />
          </div>

          <!-- Jumlah & Kesulitan -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="label">Jumlah Soal</label>
              <input v-model.number="form.count" type="number" min="1" max="20" class="input" />
              <p class="text-xs text-gray-400 mt-1">Maksimal 20 soal</p>
            </div>
            <div>
              <label class="label">Tingkat Kesulitan</label>
              <select v-model="form.difficulty" class="input">
                <option value="easy">Mudah</option>
                <option value="medium">Sedang</option>
                <option value="hard">Sulit</option>
              </select>
            </div>
          </div>

          <!-- Tipe Soal -->
          <div>
            <label class="label">Tipe Soal</label>
            <div class="flex gap-3 flex-wrap mt-1">
              <label v-for="t in typeOptions" :key="t.value"
                class="flex items-center gap-2 cursor-pointer select-none">
                <input type="checkbox" :value="t.value" v-model="form.types"
                  class="accent-blue-600 w-4 h-4" />
                <span class="text-sm text-gray-700">{{ t.label }}</span>
              </label>
            </div>
            <p v-if="form.types.length === 0" class="text-xs text-red-500 mt-1">Pilih minimal satu tipe soal.</p>
          </div>

          <p v-if="error" class="text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">{{ error }}</p>
        </div>

        <!-- Step 2: Loading -->
        <div v-else-if="step === 'loading'" class="flex flex-col items-center justify-center py-16 gap-4">
          <div class="w-12 h-12 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          <p class="text-gray-600 font-medium">Sedang generate soal...</p>
          <p class="text-gray-400 text-sm">Ini mungkin memakan waktu 30–120 detik</p>
        </div>

        <!-- Step 3: Preview hasil -->
        <div v-else-if="step === 'preview'" class="space-y-3">
          <div class="flex items-center justify-between mb-2">
            <p class="text-sm text-gray-600">
              <span class="font-semibold text-blue-600">{{ selected.size }}</span> dari
              <span class="font-semibold">{{ results.length }}</span> soal dipilih
            </p>
            <label class="flex items-center gap-2 text-sm text-gray-600 cursor-pointer select-none">
              <input type="checkbox" :checked="allSelected" @change="toggleAll" class="accent-blue-600" />
              Pilih semua
            </label>
          </div>

          <div v-for="(q, idx) in results" :key="idx"
            class="border rounded-xl overflow-hidden transition-colors"
            :class="selected.has(idx) ? 'border-blue-400' : 'border-gray-200'">

            <div class="flex items-start gap-3 p-4">
              <input type="checkbox" :checked="selected.has(idx)"
                @change="toggleSelect(idx)" class="mt-1 accent-blue-600 shrink-0 w-4 h-4" />
              <div class="flex-1 min-w-0">
                <!-- Badge tipe + poin -->
                <div class="flex items-center gap-2 mb-2 flex-wrap">
                  <span :class="typeClass(q.type)" class="text-xs font-medium px-2 py-0.5 rounded-full">
                    {{ typeLabel(q.type) }}
                  </span>
                  <span class="text-xs text-gray-400">{{ q.points }} poin</span>
                </div>
                <!-- Teks soal -->
                <p class="text-sm font-medium text-gray-800">{{ q.text }}</p>
                <!-- Opsi (MC & TF) -->
                <div v-if="q.options?.length" class="mt-2 space-y-1">
                  <div v-for="(o, oi) in q.options" :key="oi"
                    class="text-xs flex items-center gap-1.5"
                    :class="o.isCorrect ? 'text-green-700 font-medium' : 'text-gray-500'">
                    <span>{{ o.isCorrect ? '✓' : '○' }}</span>
                    {{ o.text }}
                  </div>
                </div>
                <!-- Penjelasan -->
                <p v-if="q.explanation" class="mt-2 text-xs text-blue-600 bg-blue-50 rounded px-2 py-1">
                  💡 {{ q.explanation }}
                </p>
              </div>
            </div>
          </div>
        </div>

      </div>

      <!-- Footer -->
      <div class="p-5 border-t shrink-0 flex items-center justify-between gap-3">
        <template v-if="step === 'form'">
          <p class="text-xs text-gray-400">Soal akan dibuat dalam Bahasa Indonesia</p>
          <div class="flex gap-2">
            <button @click="$emit('close')" class="btn-outline btn-sm">Batal</button>
            <button @click="generate" :disabled="!canGenerate"
              class="btn-primary btn-sm flex items-center gap-2">
              ✨ Generate
            </button>
          </div>
        </template>

        <template v-else-if="step === 'preview'">
          <button @click="step = 'form'" class="btn-outline btn-sm">← Ulangi</button>
          <div class="flex gap-2">
            <button @click="$emit('close')" class="btn-outline btn-sm">Batal</button>
            <button @click="save" :disabled="selected.size === 0 || saving"
              class="btn-primary btn-sm">
              {{ saving ? 'Menyimpan...' : `Simpan ${selected.size} Soal` }}
            </button>
          </div>
        </template>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { aiApi } from '@/api/ai'

const props = defineProps({
  model:        { type: String,  default: 'Meta-Llama-3.3-70B-Instruct' },
  providerName: { type: String,  default: 'DekaLLM' },
  courseId:     { type: Number,  default: null },
})

const emit = defineEmits(['close', 'save'])

// ── Form state ────────────────────────────────────────────────────────────────
const form = ref({
  topic:      '',
  count:      10,
  difficulty: 'medium',
  types:      ['MultipleChoice'],
})

const typeOptions = [
  { value: 'MultipleChoice', label: 'Pilihan Ganda' },
  { value: 'TrueFalse',      label: 'Benar/Salah'  },
  { value: 'Essay',          label: 'Essay'         },
]

// ── Steps: form | loading | preview ──────────────────────────────────────────
const step    = ref('form')
const error   = ref('')
const results = ref([])
const selected = ref(new Set())
const saving  = ref(false)

const canGenerate = computed(() =>
  form.value.topic.trim().length > 0 && form.value.types.length > 0
)

const allSelected = computed(() =>
  results.value.length > 0 && results.value.every((_, i) => selected.value.has(i))
)

// ── Actions ───────────────────────────────────────────────────────────────────
async function generate() {
  if (!canGenerate.value) return
  error.value = ''
  step.value  = 'loading'
  try {
    const { data } = await aiApi.generateQuestions({
      topic:      form.value.topic.trim(),
      count:      form.value.count,
      types:      form.value.types,
      difficulty: form.value.difficulty,
      courseId:   props.courseId ?? undefined,
    })
    results.value  = data
    selected.value = new Set(data.map((_, i) => i)) // pilih semua by default
    step.value     = 'preview'
  } catch (e) {
    error.value = e.response?.data?.message ?? 'Gagal generate soal. Coba lagi.'
    step.value  = 'form'
  }
}

function toggleSelect(idx) {
  const s = new Set(selected.value)
  s.has(idx) ? s.delete(idx) : s.add(idx)
  selected.value = s
}

function toggleAll() {
  if (allSelected.value) {
    selected.value = new Set()
  } else {
    selected.value = new Set(results.value.map((_, i) => i))
  }
}

async function save() {
  const toSave = results.value.filter((_, i) => selected.value.has(i))
  saving.value = true
  try {
    await emit('save', toSave)
  } finally {
    saving.value = false
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
function typeLabel(type) {
  if (type === 'MultipleChoice') return 'Pilihan Ganda'
  if (type === 'TrueFalse')      return 'Benar/Salah'
  return 'Essay'
}

function typeClass(type) {
  if (type === 'MultipleChoice') return 'bg-blue-100 text-blue-700'
  if (type === 'TrueFalse')      return 'bg-yellow-100 text-yellow-700'
  return 'bg-purple-100 text-purple-700'
}
</script>

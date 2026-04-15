<template>
  <div class="max-w-4xl mx-auto p-6">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold">Bank Soal</h1>
      <button @click="showForm = !showForm" class="btn-primary">+ Tambah Soal</button>
    </div>

    <!-- Add Form -->
    <div v-if="showForm" class="card p-5 mb-6">
      <h3 class="font-semibold mb-3">Soal Baru</h3>
      <div class="space-y-3">
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="label">Kategori</label>
            <input v-model="newQ.category" class="input" placeholder="mis: Matematika, IPA..." />
          </div>
          <div>
            <label class="label">Tipe</label>
            <select v-model="newQ.type" @change="onTypeChange" class="select">
              <option value="MultipleChoice">Pilihan Ganda</option>
              <option value="TrueFalse">Benar/Salah</option>
              <option value="Essay">Essay</option>
            </select>
          </div>
        </div>
        <div>
          <label class="label">Pertanyaan</label>
          <textarea v-model="newQ.text" class="textarea" rows="2"></textarea>
        </div>
        <div>
          <label class="label">Poin</label>
          <input v-model.number="newQ.points" type="number" class="input w-24" min="1" />
        </div>
        <div v-if="newQ.type === 'TrueFalse'" class="text-sm text-gray-500 italic">
          Jawaban: pilih Benar atau Salah di bawah sebagai jawaban yang benar.
        </div>
        <div v-if="newQ.type !== 'Essay'">
          <label class="label">{{ newQ.type === 'TrueFalse' ? 'Pilih jawaban benar' : 'Opsi (pilih jawaban benar)' }}</label>
          <div v-for="(o, i) in newQ.options" :key="i" class="flex gap-2 mb-2">
            <input type="radio" :checked="o.isCorrect"
              @change="newQ.options.forEach((x, j) => x.isCorrect = j === i)" class="mt-2.5" />
            <input v-model="o.text" class="input" :placeholder="`Opsi ${i + 1}`" />
            <button type="button" @click="newQ.options.splice(i, 1)" class="text-red-400 hover:text-red-600">✕</button>
          </div>
          <button v-if="newQ.type === 'MultipleChoice'" type="button" @click="newQ.options.push({ text: '', isCorrect: false })" class="btn-outline btn-sm">+ Opsi</button>
        </div>
        <div class="flex gap-2">
          <button @click="saveToBank" :disabled="saving" class="btn-primary btn-sm">
            {{ saving ? 'Menyimpan...' : 'Simpan ke Bank' }}
          </button>
          <button @click="showForm = false" class="btn-outline btn-sm">Batal</button>
        </div>
      </div>
    </div>

    <!-- Filter -->
    <div class="flex gap-3 mb-4">
      <input v-model="catFilter" @input="load" class="input max-w-xs" placeholder="Filter kategori..." />
    </div>

    <!-- List -->
    <div class="space-y-3">
      <div v-for="q in items" :key="q.id" class="card p-4 flex items-start justify-between gap-4">
        <div class="flex-1">
          <div class="flex gap-2 mb-1">
            <span v-if="q.category" class="badge-blue badge">{{ q.category }}</span>
            <span class="badge-gray badge">{{ q.type }}</span>
            <span class="text-xs text-gray-400">{{ q.points }} poin</span>
          </div>
          <p class="text-sm text-gray-800">{{ q.text }}</p>
          <div v-if="q.options?.length" class="mt-1.5 flex flex-wrap gap-1">
            <span v-for="o in q.options" :key="o.id"
              :class="['text-xs px-2 py-0.5 rounded', o.isCorrect ? 'bg-green-100 text-green-700 font-medium' : 'bg-gray-100 text-gray-500']">
              {{ o.text }}
            </span>
          </div>
        </div>
        <button @click="del(q.id)" class="text-red-400 hover:text-red-600 text-sm shrink-0">Hapus</button>
      </div>
      <div v-if="loadError" class="card p-6 text-center text-red-500 text-sm">
        {{ loadError }}
      </div>
      <div v-else-if="items.length === 0" class="card p-12 text-center text-gray-400">
        Bank soal kosong. Tambahkan soal pertama Anda!
      </div>
    </div>

    <Pagination :model-value="page" :total-pages="totalPages" @change="p => { page = p; load() }" />
  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { quizzesApi } from '@/api/quizzes'
import Pagination from '@/components/common/Pagination.vue'

const route = useRoute()

const items = ref([])
const page = ref(1)
const totalPages = ref(1)
const showForm = ref(false)
const saving = ref(false)
const catFilter = ref('')
const loadError = ref('')

const newQ = reactive({
  text: '', type: 'MultipleChoice', category: '', points: 1,
  options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
})

function onTypeChange() {
  if (newQ.type === 'TrueFalse') {
    newQ.options = [{ text: 'Benar', isCorrect: true }, { text: 'Salah', isCorrect: false }]
  } else if (newQ.type === 'MultipleChoice') {
    newQ.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
  } else {
    newQ.options = [] // Essay — tidak perlu opsi
  }
}

async function load() {
  loadError.value = ''
  try {
    const params = { page: page.value }
    if (catFilter.value) params.category = catFilter.value
    const { data } = await quizzesApi.getBank(params)
    items.value = data.items ?? []
    totalPages.value = data.totalPages ?? 1
  } catch (e) {
    loadError.value = e?.response?.data?.message ?? 'Gagal memuat bank soal.'
  }
}

async function saveToBank() {
  if (!newQ.text.trim()) return alert('Pertanyaan wajib diisi.')
  saving.value = true
  try {
    await quizzesApi.addToBank({ ...newQ })
    showForm.value = false
    newQ.text = ''
    newQ.category = ''
    newQ.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? e?.response?.data?.errors
      ? JSON.stringify(e.response.data.errors)
      : 'Gagal menyimpan soal. Pastikan semua field terisi.')
  } finally {
    saving.value = false
  }
}

async function del(id) {
  if (!confirm('Hapus soal dari bank?')) return
  try {
    await quizzesApi.deleteFromBank(id)
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus soal.')
  }
}

onMounted(() => {
  if (route.query.new === '1') showForm.value = true
  load()
})
</script>

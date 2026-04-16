<template>
  <div class="p-6 max-w-5xl mx-auto">
    <!-- Header -->
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Ujian Wajib</h1>
        <p class="text-sm text-gray-500 mt-0.5">Kelola ujian berbasis deep link per user</p>
      </div>
      <button @click="showCreate = true"
        class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Buat Ujian Wajib
      </button>
    </div>

    <!-- Create Modal -->
    <div v-if="showCreate" class="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div class="bg-white rounded-2xl shadow-xl w-full max-w-lg p-6">
        <h3 class="font-semibold text-lg mb-4">Buat Ujian Wajib</h3>
        <div class="space-y-3">
          <div>
            <label class="label">Judul *</label>
            <input v-model="form.title" class="input" placeholder="Judul ujian" />
          </div>
          <div>
            <label class="label">Deskripsi</label>
            <textarea v-model="form.description" class="textarea" rows="2" placeholder="Opsional"></textarea>
          </div>
          <div class="grid grid-cols-3 gap-3">
            <div>
              <label class="label">Batas Waktu (menit)</label>
              <input v-model.number="form.timeLimitMinutes" type="number" class="input" placeholder="Kosongkan = tanpa batas" min="1" />
            </div>
            <div>
              <label class="label">Maks Percobaan</label>
              <input v-model.number="form.maxAttempts" type="number" class="input" min="1" />
            </div>
            <div>
              <label class="label">Nilai Lulus (%)</label>
              <input v-model.number="form.passScore" type="number" class="input" min="0" max="100" />
            </div>
          </div>
        </div>
        <div class="flex gap-2 mt-5">
          <button @click="createExam" :disabled="saving" class="btn-primary btn-sm flex-1">
            {{ saving ? 'Menyimpan...' : 'Simpan' }}
          </button>
          <button @click="showCreate = false" class="btn-outline btn-sm flex-1">Batal</button>
        </div>
      </div>
    </div>

    <!-- List -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Memuat...</div>
    <div v-else-if="exams.length === 0" class="text-center py-16 text-gray-400">
      <p>Belum ada ujian wajib.</p>
    </div>
    <div v-else class="space-y-4">
      <div v-for="exam in exams" :key="exam.id"
        class="bg-white rounded-xl border border-gray-200 p-5">
        <div class="flex items-start justify-between gap-4 mb-3">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-0.5">
              <h3 class="font-semibold text-gray-900 truncate">{{ exam.title }}</h3>
              <span :class="exam.isActive ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'"
                class="text-xs font-medium px-2 py-0.5 rounded-full shrink-0">
                {{ exam.isActive ? 'Aktif' : 'Nonaktif' }}
              </span>
            </div>
            <p v-if="exam.description" class="text-sm text-gray-500 line-clamp-1">{{ exam.description }}</p>
          </div>
          <div class="flex gap-2 shrink-0">
            <button @click="openDetail(exam)"
              class="text-xs px-3 py-1.5 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              Kelola
            </button>
            <button @click="toggleActive(exam)"
              class="text-xs px-3 py-1.5 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
              {{ exam.isActive ? 'Nonaktifkan' : 'Aktifkan' }}
            </button>
            <button @click="deleteExam(exam)"
              class="text-xs px-3 py-1.5 border border-red-200 text-red-500 rounded-lg hover:bg-red-50 transition-colors">
              Hapus
            </button>
          </div>
        </div>
        <div class="flex gap-4 text-xs text-gray-500">
          <span>{{ exam.questionCount }} soal</span>
          <span>·</span>
          <span>{{ exam.timeLimitMinutes ? `${exam.timeLimitMinutes} menit` : 'Tanpa batas waktu' }}</span>
          <span>·</span>
          <span>Lulus ≥{{ exam.passScore }}%</span>
          <span>·</span>
          <span>{{ exam.assignmentCount }} peserta</span>
        </div>
      </div>
    </div>

    <!-- Detail Drawer -->
    <div v-if="selected" class="fixed inset-0 z-50 flex">
      <div class="flex-1 bg-black/40" @click="selected = null" />
      <div class="w-full max-w-2xl bg-white flex flex-col overflow-hidden shadow-xl">
        <div class="flex items-center justify-between p-5 border-b">
          <h3 class="font-semibold text-gray-900">{{ selected.title }}</h3>
          <button @click="selected = null" class="text-gray-400 hover:text-gray-600">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div class="flex-1 overflow-y-auto">
          <!-- Tabs -->
          <div class="flex border-b">
            <button v-for="tab in ['Soal', 'Peserta', 'Generate Link', 'Riwayat Link']" :key="tab"
              @click="activeTab = tab; if (tab === 'Riwayat Link') loadSessions()"
              :class="activeTab === tab ? 'border-b-2 border-blue-600 text-blue-600' : 'text-gray-500'"
              class="px-5 py-3 text-sm font-medium transition-colors">
              {{ tab }}
            </button>
          </div>

          <!-- Tab: Soal -->
          <div v-if="activeTab === 'Soal'" class="p-5">
            <!-- Add Question -->
            <button @click="showAddQ = !showAddQ" class="btn-outline btn-sm mb-4">
              {{ showAddQ ? 'Batal' : '+ Tambah Soal' }}
            </button>
            <div v-if="showAddQ" class="card p-4 mb-4">
              <div class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="label">Tipe</label>
                    <select v-model="qForm.type" @change="onQTypeChange" class="select">
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
                  <textarea v-model="qForm.text" class="textarea" rows="2"></textarea>
                </div>
                <div v-if="qForm.type !== 'Essay'">
                  <label class="label">Opsi</label>
                  <div v-for="(o, i) in qForm.options" :key="i" class="flex gap-2 mb-2">
                    <input type="radio" :checked="o.isCorrect"
                      @change="qForm.options.forEach((x, j) => x.isCorrect = j === i)" class="mt-2.5" />
                    <input v-model="o.text" class="input" :placeholder="`Opsi ${i + 1}`" />
                    <button v-if="qForm.type === 'MultipleChoice'" type="button"
                      @click="qForm.options.splice(i, 1)" class="text-red-400 hover:text-red-600">✕</button>
                  </div>
                  <button v-if="qForm.type === 'MultipleChoice'" type="button"
                    @click="qForm.options.push({ text: '', isCorrect: false })" class="btn-outline btn-sm">+ Opsi</button>
                </div>
                <button @click="addQuestion" :disabled="savingQ" class="btn-primary btn-sm">
                  {{ savingQ ? 'Menyimpan...' : 'Simpan Soal' }}
                </button>
              </div>
            </div>

            <div v-if="detailLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="detail?.questions?.length === 0" class="text-gray-400 text-sm">Belum ada soal.</div>
            <div v-else class="space-y-2">
              <div v-for="q in detail?.questions" :key="q.id"
                class="p-3 border rounded-lg flex items-start justify-between gap-3">
                <div>
                  <p class="text-sm text-gray-800">{{ q.order + 1 }}. {{ q.text }}</p>
                  <p class="text-xs text-gray-400 mt-1">{{ q.type }} · {{ q.points }} poin</p>
                  <div v-if="q.options?.length" class="flex flex-wrap gap-1 mt-1">
                    <span v-for="o in q.options" :key="o.id"
                      :class="['text-xs px-1.5 py-0.5 rounded', o.isCorrect ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500']">
                      {{ o.text }}
                    </span>
                  </div>
                </div>
                <button @click="deleteQuestion(q.id)" class="text-red-400 hover:text-red-600 text-xs shrink-0">Hapus</button>
              </div>
            </div>
          </div>

          <!-- Tab: Peserta -->
          <div v-if="activeTab === 'Peserta'" class="p-5">
            <!-- Assign form -->
            <div class="flex gap-2 mb-4">
              <input v-model="assignUserId" class="input flex-1" placeholder="User ID" />
              <input v-model="assignUserName" class="input flex-1" placeholder="Nama user" />
              <button @click="assignUser" :disabled="assigning" class="btn-primary btn-sm shrink-0">
                {{ assigning ? '...' : 'Assign' }}
              </button>
            </div>
            <div v-if="detailLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="detail?.assignments?.length === 0" class="text-gray-400 text-sm">Belum ada peserta.</div>
            <table v-else class="w-full text-sm">
              <thead>
                <tr class="text-left text-xs text-gray-500 border-b">
                  <th class="pb-2">User</th>
                  <th class="pb-2">Status</th>
                  <th class="pb-2">Percobaan</th>
                  <th class="pb-2"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100">
                <tr v-for="a in detail?.assignments" :key="a.id" class="py-2">
                  <td class="py-2 font-medium">{{ a.userName }}<br/><span class="text-xs text-gray-400">{{ a.userId }}</span></td>
                  <td class="py-2">
                    <span :class="{
                      'bg-gray-100 text-gray-500': a.status === 'NotYet',
                      'bg-yellow-100 text-yellow-700': a.status === 'InProgress',
                      'bg-green-100 text-green-700': a.status === 'Done',
                    }" class="text-xs px-2 py-0.5 rounded-full font-medium">
                      {{ { NotYet: 'Belum', InProgress: 'Sedang', Done: 'Selesai' }[a.status] }}
                    </span>
                  </td>
                  <td class="py-2 text-gray-500">{{ a.attemptCount }}</td>
                  <td class="py-2">
                    <button @click="unassignUser(a.userId)" class="text-xs text-red-400 hover:text-red-600">Hapus</button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Tab: Riwayat Link -->
          <div v-if="activeTab === 'Riwayat Link'" class="p-5">
            <div class="flex justify-between items-center mb-4">
              <p class="text-sm text-gray-600">Semua link yang pernah digenerate untuk ujian ini.</p>
              <button @click="loadSessions" class="text-xs text-blue-600 hover:underline">Refresh</button>
            </div>
            <div v-if="sessionsLoading" class="text-gray-400 text-sm">Memuat...</div>
            <div v-else-if="sessions.length === 0" class="text-gray-400 text-sm">Belum ada link yang digenerate.</div>
            <div v-else class="space-y-2">
              <div v-for="s in sessions" :key="s.id"
                class="p-3 border rounded-lg text-xs flex items-start justify-between gap-3"
                :class="s.isRevoked ? 'bg-red-50 border-red-200' : s.isExpired ? 'bg-gray-50 border-gray-200' : 'border-green-200 bg-green-50'">
                <div class="space-y-0.5">
                  <div class="flex items-center gap-2">
                    <span :class="s.isRevoked ? 'bg-red-100 text-red-700' : s.isExpired ? 'bg-gray-200 text-gray-500' : 'bg-green-100 text-green-700'"
                      class="px-1.5 py-0.5 rounded font-medium">
                      {{ s.isRevoked ? 'Dicabut' : s.isExpired ? 'Expired' : 'Aktif' }}
                    </span>
                    <span class="text-gray-600">User: <b>{{ s.userId }}</b></span>
                  </div>
                  <p class="text-gray-400">Generate: {{ formatDate(s.generatedAt) }}</p>
                  <p class="text-gray-400">Berlaku sampai: {{ formatDate(s.expiresAt) }}</p>
                  <p class="text-gray-400">
                    Pertama dipakai: {{ s.usedAt ? formatDate(s.usedAt) : 'Belum pernah dipakai' }}
                  </p>
                </div>
                <button v-if="!s.isRevoked && !s.isExpired"
                  @click="revokeSession(s)"
                  class="text-red-500 hover:text-red-700 shrink-0 border border-red-200 px-2 py-1 rounded hover:bg-red-50">
                  Cabut
                </button>
              </div>
            </div>
          </div>

          <!-- Tab: Generate Link -->
          <div v-if="activeTab === 'Generate Link'" class="p-5">
            <p class="text-sm text-gray-600 mb-4">
              Generate deep link untuk user tertentu. Link berlaku sesuai durasi yang dipilih dan langsung masuk ke exam.
            </p>
            <div class="space-y-3 mb-4">
              <div>
                <label class="label">User ID *</label>
                <input v-model="linkUserId" class="input" placeholder="ID user yang akan menerima link" />
              </div>
              <div>
                <label class="label">Berlaku selama (menit)</label>
                <input v-model.number="linkExpiry" type="number" class="input w-32" min="10" max="1440" />
              </div>
              <button @click="generateLink" :disabled="generatingLink" class="btn-primary btn-sm">
                {{ generatingLink ? 'Generating...' : 'Generate Link' }}
              </button>
            </div>

            <div v-if="generatedLink" class="p-4 bg-gray-50 rounded-xl border border-gray-200">
              <p class="text-xs text-gray-500 mb-1">Deep Link:</p>
              <div class="flex gap-2 items-center">
                <p class="text-xs font-mono text-blue-600 flex-1 break-all">{{ generatedLink.deepLink }}</p>
                <button @click="copyLink" class="text-xs text-gray-500 hover:text-gray-700 shrink-0 border px-2 py-1 rounded">
                  Salin
                </button>
              </div>
              <p class="text-xs text-gray-400 mt-2">Berlaku hingga: {{ formatDate(generatedLink.expiresAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { mandatoryExamApi } from '@/api/mandatoryExam'

const exams   = ref([])
const loading = ref(true)
const saving  = ref(false)
const showCreate = ref(false)

const form = reactive({
  title: '', description: '', timeLimitMinutes: null, maxAttempts: 1, passScore: 60
})

// Detail drawer
const selected    = ref(null)
const detail      = ref(null)
const detailLoading = ref(false)
const activeTab   = ref('Soal')

// Add question
const showAddQ = ref(false)
const savingQ  = ref(false)
const qForm    = reactive({
  type: 'MultipleChoice', text: '', points: 10,
  options: [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
})

// Assign
const assignUserId   = ref('')
const assignUserName = ref('')
const assigning      = ref(false)

// Generate link
const linkUserId      = ref('')
const linkExpiry      = ref(60)
const generatingLink  = ref(false)
const generatedLink   = ref(null)

// Sessions
const sessions        = ref([])
const sessionsLoading = ref(false)

onMounted(load)

async function load() {
  loading.value = true
  try {
    const { data } = await mandatoryExamApi.getAll()
    exams.value = data
  } finally {
    loading.value = false
  }
}

async function createExam() {
  if (!form.title.trim()) return alert('Judul wajib diisi.')
  saving.value = true
  try {
    await mandatoryExamApi.create({ ...form })
    showCreate.value = false
    form.title = ''; form.description = ''; form.timeLimitMinutes = null; form.maxAttempts = 1; form.passScore = 60
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal membuat ujian.')
  } finally {
    saving.value = false
  }
}

async function toggleActive(exam) {
  try {
    await mandatoryExamApi.toggleActive(exam.id)
    exam.isActive = !exam.isActive
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mengubah status.')
  }
}

async function deleteExam(exam) {
  if (!confirm(`Hapus "${exam.title}"? Semua data akan ikut terhapus.`)) return
  try {
    await mandatoryExamApi.delete(exam.id)
    exams.value = exams.value.filter(e => e.id !== exam.id)
    if (selected.value?.id === exam.id) selected.value = null
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus.')
  }
}

async function openDetail(exam) {
  selected.value      = exam
  activeTab.value     = 'Soal'
  detailLoading.value = true
  detail.value        = null
  generatedLink.value = null
  sessions.value      = []
  try {
    const { data } = await mandatoryExamApi.getById(exam.id)
    detail.value = data
  } finally {
    detailLoading.value = false
  }
}

async function loadSessions() {
  if (!selected.value) return
  sessionsLoading.value = true
  try {
    const { data } = await mandatoryExamApi.getSessions(selected.value.id)
    sessions.value = data
  } finally {
    sessionsLoading.value = false
  }
}

async function revokeSession(s) {
  if (!confirm('Cabut link ini? User tidak akan bisa menggunakan link tersebut lagi.')) return
  try {
    await mandatoryExamApi.revokeSession(s.id)
    s.isRevoked = true
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal mencabut link.')
  }
}

function onQTypeChange() {
  if (qForm.type === 'TrueFalse') {
    qForm.options = [{ text: 'Benar', isCorrect: true }, { text: 'Salah', isCorrect: false }]
  } else if (qForm.type === 'MultipleChoice') {
    qForm.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
  } else {
    qForm.options = []
  }
}

async function addQuestion() {
  if (!qForm.text.trim()) return alert('Pertanyaan wajib diisi.')
  savingQ.value = true
  try {
    await mandatoryExamApi.addQuestion(selected.value.id, { ...qForm })
    showAddQ.value = false
    qForm.text = ''; qForm.type = 'MultipleChoice'; qForm.points = 10
    qForm.options = [{ text: '', isCorrect: true }, { text: '', isCorrect: false }]
    const { data } = await mandatoryExamApi.getById(selected.value.id)
    detail.value = data
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menyimpan soal.')
  } finally {
    savingQ.value = false
  }
}

async function deleteQuestion(qId) {
  if (!confirm('Hapus soal ini?')) return
  try {
    await mandatoryExamApi.deleteQuestion(selected.value.id, qId)
    detail.value.questions = detail.value.questions.filter(q => q.id !== qId)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus soal.')
  }
}

async function assignUser() {
  if (!assignUserId.value.trim() || !assignUserName.value.trim()) return alert('User ID dan nama wajib diisi.')
  assigning.value = true
  try {
    await mandatoryExamApi.assign(selected.value.id, { userId: assignUserId.value, userName: assignUserName.value })
    assignUserId.value = ''; assignUserName.value = ''
    const { data } = await mandatoryExamApi.getById(selected.value.id)
    detail.value = data
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal assign user.')
  } finally {
    assigning.value = false
  }
}

async function unassignUser(userId) {
  if (!confirm('Hapus user dari ujian ini?')) return
  try {
    await mandatoryExamApi.unassign(selected.value.id, userId)
    detail.value.assignments = detail.value.assignments.filter(a => a.userId !== userId)
    await load()
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus user.')
  }
}

async function generateLink() {
  if (!linkUserId.value.trim()) return alert('User ID wajib diisi.')
  generatingLink.value = true
  try {
    const { data } = await mandatoryExamApi.generateLink(selected.value.id, {
      userId: linkUserId.value, expiryMinutes: linkExpiry.value
    })
    generatedLink.value = data
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal generate link.')
  } finally {
    generatingLink.value = false
  }
}

function copyLink() {
  navigator.clipboard.writeText(generatedLink.value.deepLink)
    .then(() => alert('Link disalin!'))
}

function formatDate(d) {
  return new Date(d).toLocaleString('id-ID', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>

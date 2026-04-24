<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-xl font-bold">{{ isEdit ? 'Edit Modul' : 'Buat Modul Baru' }}</h1>
    </div>

    <div class="card p-6 space-y-4">
      <form @submit.prevent="save" class="space-y-4">
        <div>
          <label class="label">Judul</label>
          <input v-model="form.title" class="input" required />
        </div>

        <div>
          <label class="label">Konten (HTML)</label>
          <textarea v-model="form.content" class="textarea" rows="8" placeholder="Isi materi dalam format HTML..."></textarea>
        </div>

        <div>
          <label class="label">URL Video (YouTube/Vimeo)</label>
          <input v-model="form.videoUrl" class="input" placeholder="https://youtube.com/watch?v=..." />
          <p class="text-xs text-gray-400 mt-1">Paste link YouTube atau Vimeo — video akan di-embed otomatis.</p>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="label">Urutan</label>
            <input v-model.number="form.order" type="number" class="input" min="0" />
          </div>
          <div>
            <label class="label">Durasi (menit)</label>
            <input v-model.number="form.durationMinutes" type="number" class="input" min="0" />
          </div>
        </div>

        <div class="flex items-center gap-2">
          <input v-model="form.isPublished" type="checkbox" class="w-4 h-4" id="pub" />
          <label for="pub" class="text-sm">Publikasikan modul ini</label>
        </div>

        <!-- Error simpan -->
        <div v-if="saveError" class="p-3 rounded-lg bg-red-50 border border-red-200 text-sm text-red-700">
          {{ saveError }}
        </div>

        <div class="flex gap-3 items-center">
          <button type="submit" :disabled="saving" class="btn-primary">
            <span v-if="saving" class="flex items-center gap-2">
              <div class="animate-spin rounded-full h-4 w-4 border-2 border-white border-t-transparent"></div>
              {{ uploadProgress || 'Menyimpan...' }}
            </span>
            <span v-else>
              {{ isEdit ? 'Simpan Perubahan' : (pendingFiles.length ? `Simpan + Upload ${pendingFiles.length} File` : 'Simpan') }}
            </span>
          </button>
          <button type="button" @click="$router.back()" class="btn-outline">Batal</button>
        </div>
      </form>
    </div>

    <!-- Lampiran File — tampil di create dan edit mode -->
    <div class="card p-6 mt-6">
      <h2 class="font-semibold text-gray-900 mb-1">Lampiran File</h2>
      <p class="text-xs text-gray-400 mb-4">
        Format didukung: PDF, Word, PowerPoint, Excel, ZIP, MP4, MP3 · Maks 100 MB per file
      </p>

      <!-- Upload area -->
      <div
        class="border-2 border-dashed rounded-lg p-6 text-center transition cursor-pointer"
        :class="isDragging ? 'border-blue-400 bg-blue-50' : 'border-gray-300 hover:border-blue-400'"
        @click="fileInput.click()"
        @dragover.prevent="isDragging = true"
        @dragleave.prevent="isDragging = false"
        @drop.prevent="onDrop"
      >
        <svg class="w-10 h-10 text-gray-300 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
        </svg>
        <p class="text-sm text-gray-500">Klik atau drag & drop file ke sini</p>
        <input ref="fileInput" type="file" class="hidden"
          accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.zip,.mp4,.mp3,.jpg,.jpeg,.png,.gif,.webp"
          multiple
          @change="onFileChange" />
      </div>

      <!-- Error validasi -->
      <div v-if="uploadError" class="mt-2 text-sm text-red-600">{{ uploadError }}</div>

      <!-- CREATE MODE: file yang sudah dipilih (antrian, belum diupload) -->
      <div v-if="!isEdit && pendingFiles.length" class="mt-4 space-y-2">
        <p class="text-xs text-gray-500 font-medium">File akan diupload saat Simpan:</p>
        <div v-for="(f, i) in pendingFiles" :key="i"
          class="flex items-center justify-between p-3 rounded-lg border border-blue-100 bg-blue-50">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-8 h-8 rounded bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-bold uppercase shrink-0">
              {{ getExt(f.name) }}
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-gray-800 truncate">{{ f.name }}</p>
              <p class="text-xs text-gray-400">{{ formatSize(f.size) }}</p>
            </div>
          </div>
          <button type="button" @click="pendingFiles.splice(i, 1)"
            class="text-red-400 hover:text-red-600 ml-3 shrink-0 transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>
      </div>

      <!-- EDIT MODE: upload progress -->
      <div v-if="isEdit && uploading" class="mt-3 flex items-center gap-2 text-sm text-blue-600">
        <div class="animate-spin rounded-full h-4 w-4 border-2 border-blue-600 border-t-transparent"></div>
        Mengupload {{ uploadingName }}...
      </div>

      <!-- EDIT MODE: daftar lampiran yang sudah ada -->
      <div v-if="isEdit && attachments.length" class="mt-4 space-y-2">
        <div v-for="att in attachments" :key="att.id"
          class="flex items-center justify-between p-3 rounded-lg border border-gray-100 bg-gray-50">
          <div class="flex items-center gap-3 min-w-0">
            <div class="w-8 h-8 rounded bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-bold uppercase shrink-0">
              {{ att.fileType }}
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-gray-800 truncate">{{ att.fileName }}</p>
              <p class="text-xs text-gray-400">{{ formatSize(att.fileSize) }}</p>
            </div>
          </div>
          <button type="button" @click="deleteAttachment(att)"
            class="text-red-400 hover:text-red-600 ml-3 shrink-0 transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
            </svg>
          </button>
        </div>
      </div>

      <p v-if="isEdit && !attachments.length && !uploading" class="mt-4 text-sm text-gray-400 text-center">
        Belum ada lampiran.
      </p>
      <p v-if="!isEdit && !pendingFiles.length" class="mt-4 text-sm text-gray-400 text-center">
        Pilih file di atas untuk menambahkan lampiran.
      </p>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { modulesApi } from '@/api/courses'

const route = useRoute()
const router = useRouter()

const isEdit = computed(() => !!route.params.id && route.params.id !== 'create')
const saving = ref(false)
const uploadProgress = ref('')
const saveError = ref('')
const form = ref({ title: '', content: '', videoUrl: '', order: 0, isPublished: true, durationMinutes: 0 })

// Attachment state
const fileInput = ref(null)
const attachments = ref([])       // Sudah diupload (edit mode)
const pendingFiles = ref([])      // Antrian lokal belum diupload (create mode)
const uploading = ref(false)
const uploadingName = ref('')
const uploadError = ref('')
const isDragging = ref(false)

const ALLOWED = ['.pdf', '.doc', '.docx', '.ppt', '.pptx', '.xls', '.xlsx', '.zip', '.mp4', '.mp3', '.jpg', '.jpeg', '.png', '.gif', '.webp']
const MAX_SIZE = 100 * 1024 * 1024

async function save() {
  saving.value = true
  saveError.value = ''

  try {
    if (isEdit.value) {
      await modulesApi.update(route.params.courseId, route.params.id, form.value)
      router.push(`/courses/${route.params.courseId}`)
      return
    }

    // — Buat modul —
    let createdId
    try {
      const { data } = await modulesApi.create(route.params.courseId, form.value)
      createdId = data.id
    } catch (e) {
      saveError.value = e.response?.data?.message
        || `Gagal menyimpan modul (HTTP ${e.response?.status ?? 'network error'}).`
      return
    }

    // — Upload file satu per satu —
    for (let i = 0; i < pendingFiles.value.length; i++) {
      const f = pendingFiles.value[i]
      uploadProgress.value = `Mengupload file ${i + 1}/${pendingFiles.value.length}: ${f.name}`
      try {
        await modulesApi.uploadAttachment(route.params.courseId, createdId, f)
      } catch (e) {
        const msg = e.response?.data?.message
          || `Gagal mengupload "${f.name}" (HTTP ${e.response?.status ?? 'network error'}).`
        saveError.value = `${msg} — Modul sudah tersimpan, kamu bisa upload ulang di halaman Edit.`
        router.push(`/courses/${route.params.courseId}/modules/${createdId}/edit`)
        return
      }
    }

    router.push(`/courses/${route.params.courseId}`)
  } finally {
    saving.value = false
    uploadProgress.value = ''
  }
}

function validateFile(file) {
  const ext = '.' + file.name.split('.').pop().toLowerCase()
  if (!ALLOWED.includes(ext)) {
    uploadError.value = `Format tidak didukung: ${file.name}. Gunakan: ${ALLOWED.join(', ')}`
    return false
  }
  if (file.size > MAX_SIZE) {
    uploadError.value = `File "${file.name}" melebihi batas 100 MB.`
    return false
  }
  return true
}

function onFileChange(e) {
  const files = Array.from(e.target.files ?? [])
  fileInput.value.value = ''
  handleFiles(files)
}

function onDrop(e) {
  isDragging.value = false
  const files = Array.from(e.dataTransfer.files ?? [])
  handleFiles(files)
}

async function handleFiles(files) {
  uploadError.value = ''
  if (!isEdit.value) {
    // Create mode: tambah ke antrian lokal
    for (const f of files) {
      if (validateFile(f)) pendingFiles.value.push(f)
    }
    return
  }
  // Edit mode: upload langsung satu per satu (sequential — tunggu tiap file selesai)
  for (const f of files) {
    if (validateFile(f)) await uploadImmediate(f)
  }
}

async function uploadImmediate(file) {
  uploading.value = true
  uploadingName.value = file.name
  try {
    const { data } = await modulesApi.uploadAttachment(
      route.params.courseId, route.params.id, file
    )
    attachments.value.push(data)
  } catch {
    uploadError.value = 'Upload gagal. Coba lagi.'
  } finally {
    uploading.value = false
    uploadingName.value = ''
  }
}

async function deleteAttachment(att) {
  if (!confirm(`Hapus lampiran "${att.fileName}"?`)) return
  await modulesApi.deleteAttachment(route.params.courseId, route.params.id, att.id)
  attachments.value = attachments.value.filter(a => a.id !== att.id)
}

function getExt(filename) {
  return filename.split('.').pop().toLowerCase()
}

function formatSize(bytes) {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  if (bytes >= 1024) return `${Math.round(bytes / 1024)} KB`
  return `${bytes} B`
}

onMounted(async () => {
  if (isEdit.value) {
    const { data } = await modulesApi.getById(route.params.courseId, route.params.id)
    form.value = {
      title: data.title,
      content: data.content ?? '',
      videoUrl: data.videoUrl ?? '',
      order: data.order,
      isPublished: data.isPublished,
      durationMinutes: data.durationMinutes
    }
    if (data.attachments?.length) attachments.value = data.attachments
  }
})
</script>

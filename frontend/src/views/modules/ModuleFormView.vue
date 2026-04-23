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

        <div class="flex gap-3">
          <button type="submit" :disabled="saving" class="btn-primary">
            {{ saving ? 'Menyimpan...' : (isEdit ? 'Simpan Perubahan' : 'Simpan & Lanjut') }}
          </button>
          <button type="button" @click="$router.back()" class="btn-outline">Batal</button>
        </div>

        <!-- Info: lampiran bisa ditambah setelah modul disimpan (create mode only) -->
        <p v-if="!isEdit" class="text-xs text-gray-400 mt-1">
          Setelah disimpan, kamu bisa menambahkan lampiran file ke modul ini.
        </p>
      </form>
    </div>

    <!-- Attachment section — hanya muncul saat edit (sudah ada module ID) -->
    <div v-if="isEdit" class="card p-6 mt-6">
      <h2 class="font-semibold text-gray-900 mb-1">Lampiran File</h2>
      <p class="text-xs text-gray-400 mb-4">
        Format didukung: PDF, Word, PowerPoint, Excel, ZIP, MP4, MP3 · Maks 100 MB per file
      </p>

      <!-- Upload area -->
      <div
        class="border-2 border-dashed border-gray-300 rounded-lg p-6 text-center hover:border-blue-400 transition cursor-pointer"
        @click="fileInput.click()"
        @dragover.prevent
        @drop.prevent="onDrop"
      >
        <svg class="w-10 h-10 text-gray-300 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
        </svg>
        <p class="text-sm text-gray-500">Klik atau drag & drop file ke sini</p>
        <input ref="fileInput" type="file" class="hidden"
          accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.zip,.mp4,.mp3"
          @change="uploadFile" />
      </div>

      <!-- Upload progress -->
      <div v-if="uploading" class="mt-3 flex items-center gap-2 text-sm text-blue-600">
        <div class="animate-spin rounded-full h-4 w-4 border-2 border-blue-600 border-t-transparent"></div>
        Mengupload {{ uploadingName }}...
      </div>
      <div v-if="uploadError" class="mt-2 text-sm text-red-600">{{ uploadError }}</div>

      <!-- Existing attachments -->
      <div v-if="attachments.length" class="mt-4 space-y-2">
        <div v-for="att in attachments" :key="att.id"
          class="flex items-center justify-between p-3 rounded-lg border border-gray-100 bg-gray-50">
          <div class="flex items-center gap-3 min-w-0">
            <!-- File type icon -->
            <div class="w-8 h-8 rounded bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-bold uppercase shrink-0">
              {{ att.fileType }}
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-gray-800 truncate">{{ att.fileName }}</p>
              <p class="text-xs text-gray-400">{{ formatSize(att.fileSize) }}</p>
            </div>
          </div>
          <button @click="deleteAttachment(att)" class="text-red-400 hover:text-red-600 ml-3 shrink-0 transition">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
            </svg>
          </button>
        </div>
      </div>

      <p v-else-if="!uploading" class="mt-4 text-sm text-gray-400 text-center">Belum ada lampiran.</p>
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
const form = ref({ title: '', content: '', videoUrl: '', order: 0, isPublished: true, durationMinutes: 0 })

// Attachment state
const fileInput = ref(null)
const attachments = ref([])
const uploading = ref(false)
const uploadingName = ref('')
const uploadError = ref('')

async function save() {
  saving.value = true
  try {
    if (isEdit.value) {
      await modulesApi.update(route.params.courseId, route.params.id, form.value)
      router.push(`/courses/${route.params.courseId}`)
    } else {
      // Setelah create, redirect ke edit page agar user langsung bisa tambah lampiran
      const { data } = await modulesApi.create(route.params.courseId, form.value)
      router.push(`/courses/${route.params.courseId}/modules/${data.id}/edit`)
    }
  } catch (e) {
    alert(e.response?.data?.message || 'Gagal menyimpan modul. Silakan coba lagi.')
  } finally {
    saving.value = false
  }
}

async function uploadFile(e) {
  const file = e.target.files?.[0]
  if (!file) return
  fileInput.value.value = '' // reset DULU agar file yang sama bisa dipilih lagi
  await doUpload(file)
}

async function onDrop(e) {
  const file = e.dataTransfer.files?.[0]
  if (file) await doUpload(file)
}

async function doUpload(file) {
  const maxSize = 100 * 1024 * 1024
  const allowed = ['.pdf','.doc','.docx','.ppt','.pptx','.xls','.xlsx','.zip','.mp4','.mp3']
  const ext = '.' + file.name.split('.').pop().toLowerCase()

  if (!allowed.includes(ext)) {
    uploadError.value = `Format tidak didukung. Gunakan: ${allowed.join(', ')}`
    return
  }
  if (file.size > maxSize) {
    uploadError.value = 'File melebihi batas 100 MB.'
    return
  }

  uploadError.value = ''
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
    // Load existing attachments
    if (data.attachments?.length) {
      attachments.value = data.attachments
    }
  }
})
</script>

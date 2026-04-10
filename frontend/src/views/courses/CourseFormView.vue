<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-2xl font-bold text-gray-900">{{ isEdit ? 'Edit Kursus' : 'Buat Kursus Baru' }}</h1>
    </div>

    <AlertMessage :message="error" type="error" @dismiss="error = null" class="mb-4" />
    <AlertMessage :message="success" type="success" @dismiss="success = null" class="mb-4" />

    <!-- Form Info Kursus -->
    <div class="card p-6 mb-6">
      <form @submit.prevent="handleSubmit" class="space-y-5">
        <div>
          <label class="label">Judul Kursus *</label>
          <input v-model="form.title" type="text" class="input" placeholder="Judul kursus yang menarik..." required maxlength="200" />
        </div>

        <div>
          <label class="label">Deskripsi *</label>
          <textarea v-model="form.description" class="textarea" rows="5" placeholder="Jelaskan apa yang akan dipelajari..." required></textarea>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="label">Kategori</label>
            <input v-model="form.category" type="text" class="input" placeholder="mis: Pemrograman, Desain..." list="category-list" />
            <datalist id="category-list">
              <option v-for="c in commonCategories" :key="c" :value="c" />
            </datalist>
          </div>
          <div>
            <label class="label">Level</label>
            <select v-model="form.level" class="select">
              <option value="Semua">Semua Level</option>
              <option value="Pemula">Pemula</option>
              <option value="Menengah">Menengah</option>
              <option value="Lanjutan">Lanjutan</option>
            </select>
          </div>
        </div>

        <!-- Thumbnail -->
        <div>
          <label class="label">Thumbnail Kursus</label>
          <div class="flex items-center gap-4">
            <div class="w-32 h-20 rounded-lg border border-gray-200 overflow-hidden bg-gray-50 flex items-center justify-center">
              <img v-if="thumbnailPreview || form.thumbnailUrl" :src="thumbnailPreview || form.thumbnailUrl" class="w-full h-full object-cover" />
              <svg v-else class="w-8 h-8 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"/>
              </svg>
            </div>
            <div>
              <input ref="thumbnailInput" type="file" accept="image/*" class="hidden" @change="onThumbnailChange" />
              <button type="button" @click="thumbnailInput.click()" class="btn-outline btn-sm">Pilih Gambar</button>
              <p class="text-xs text-gray-400 mt-1">JPG, PNG, max 5MB</p>
            </div>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <input id="published" v-model="form.isPublished" type="checkbox" class="w-4 h-4 rounded text-blue-600" />
          <label for="published" class="text-sm text-gray-700">Publikasikan kursus (dapat dilihat siswa)</label>
        </div>

        <div class="flex gap-3 pt-2">
          <button type="submit" :disabled="loading" class="btn-primary">
            <svg v-if="loading" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            {{ loading ? 'Menyimpan...' : (isEdit ? 'Simpan Perubahan' : 'Buat Kursus') }}
          </button>
          <button type="button" @click="$router.back()" class="btn-outline">Batal</button>
        </div>
      </form>
    </div>

    <!-- ── Section Upload File & Video ── -->
    <div class="card p-6">
      <h2 class="font-semibold text-gray-900 mb-1">File & Video Kursus</h2>
      <p class="text-xs text-gray-400 mb-4">
        Upload materi pendukung: PDF, Word, PowerPoint, Excel, video (MP4/WebM), audio (MP3), gambar, ZIP · Maks 100 MB
      </p>

      <!-- Peringatan sebelum kursus disimpan -->
      <div v-if="!isEdit" class="bg-yellow-50 border border-yellow-200 rounded-lg p-4 text-sm text-yellow-800">
        💡 Simpan kursus terlebih dahulu, lalu kamu bisa upload file di sini.
      </div>

      <template v-else>
        <!-- Drop zone -->
        <div
          class="border-2 border-dashed border-gray-300 rounded-lg p-8 text-center hover:border-blue-400 transition cursor-pointer"
          :class="{ 'border-blue-400 bg-blue-50': isDragging }"
          @click="fileInput.click()"
          @dragover.prevent="isDragging = true"
          @dragleave="isDragging = false"
          @drop.prevent="onDrop"
        >
          <svg class="w-10 h-10 text-gray-300 mx-auto mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/>
          </svg>
          <p class="text-sm text-gray-500">Klik atau drag & drop file ke sini</p>
          <p class="text-xs text-gray-400 mt-1">PDF · Word · PPT · Excel · MP4 · WebM · MP3 · PNG · JPG · ZIP</p>
          <input ref="fileInput" type="file" class="hidden"
            accept=".pdf,.doc,.docx,.ppt,.pptx,.xls,.xlsx,.mp4,.webm,.mp3,.png,.jpg,.jpeg,.gif,.svg,.zip,.rar,.txt"
            multiple
            @change="onFileChange" />
        </div>

        <!-- Upload progress -->
        <div v-if="uploading" class="mt-3 space-y-2">
          <div v-for="(u, i) in uploadQueue" :key="i" class="flex items-center gap-2 text-sm">
            <div class="animate-spin rounded-full h-4 w-4 border-2 border-blue-600 border-t-transparent shrink-0"></div>
            <span class="text-blue-600 truncate">{{ u }}</span>
          </div>
        </div>
        <div v-if="uploadError" class="mt-2 text-sm text-red-600">{{ uploadError }}</div>

        <!-- Resource list -->
        <div v-if="resources.length" class="mt-5 space-y-2">
          <div v-for="r in resources" :key="r.id"
            class="flex items-center gap-3 p-3 rounded-lg border border-gray-100 hover:bg-gray-50 transition group">

            <!-- Icon tipe file -->
            <div :class="fileIconClass(r.fileType)" class="w-9 h-9 rounded-lg flex items-center justify-center text-xs font-bold uppercase shrink-0">
              {{ r.fileType === 'video' ? '▶' : r.fileType === 'audio' ? '♪' : r.fileType.slice(0,3) }}
            </div>

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 truncate">{{ r.title }}</p>
              <p class="text-xs text-gray-400">{{ r.fileSizeLabel }} · {{ r.fileType }} · {{ r.downloadCount }} unduhan</p>
            </div>

            <!-- Visibility toggle -->
            <button @click="toggleVisibility(r)"
              :class="r.isVisible ? 'text-green-600' : 'text-gray-300'"
              class="text-xs font-medium hover:opacity-70 transition shrink-0"
              :title="r.isVisible ? 'Klik untuk sembunyikan' : 'Klik untuk tampilkan'">
              {{ r.isVisible ? 'Tampil' : 'Tersembunyi' }}
            </button>

            <!-- Delete -->
            <button @click="deleteResource(r)"
              class="text-gray-300 hover:text-red-500 transition shrink-0 opacity-0 group-hover:opacity-100">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
              </svg>
            </button>
          </div>
        </div>

        <p v-else-if="!uploading" class="mt-4 text-sm text-gray-400 text-center">Belum ada file yang diupload.</p>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { coursesApi, resourcesApi } from '@/api/courses'
import AlertMessage from '@/components/common/AlertMessage.vue'

const route = useRoute()
const router = useRouter()

const isEdit = computed(() => !!route.params.id)
const loading = ref(false)
const error = ref(null)
const success = ref(null)
const thumbnailInput = ref(null)
const thumbnailPreview = ref(null)
const thumbnailFile = ref(null)

// Resources
const fileInput = ref(null)
const resources = ref([])
const uploading = ref(false)
const uploadQueue = ref([])
const uploadError = ref('')
const isDragging = ref(false)

const commonCategories = ['Pemrograman', 'Desain', 'Bisnis', 'Bahasa', 'Matematika', 'Sains', 'Seni', 'Olahraga']

const form = ref({
  title: '', description: '', category: '', level: 'Semua', isPublished: false, thumbnailUrl: null
})

// ── Course form ───────────────────────────────────────────────────────────────

function onThumbnailChange(e) {
  const file = e.target.files[0]
  if (!file) return
  thumbnailFile.value = file
  thumbnailPreview.value = URL.createObjectURL(file)
}

async function handleSubmit() {
  loading.value = true
  error.value = null
  try {
    let courseId
    if (isEdit.value) {
      await coursesApi.update(route.params.id, form.value)
      courseId = route.params.id
      success.value = 'Kursus berhasil diperbarui!'
    } else {
      const { data } = await coursesApi.create(form.value)
      courseId = data.id
    }

    if (thumbnailFile.value) {
      await coursesApi.uploadThumbnail(courseId, thumbnailFile.value)
    }

    router.push(`/courses/${courseId}/edit`)
  } catch (e) {
    error.value = e.response?.data?.message || 'Gagal menyimpan kursus.'
  } finally {
    loading.value = false
  }
}

// ── Resources ─────────────────────────────────────────────────────────────────

async function loadResources() {
  if (!isEdit.value) return
  try {
    const { data } = await resourcesApi.getAll(route.params.id)
    resources.value = data
  } catch {}
}

function onFileChange(e) {
  const files = [...e.target.files]
  if (files.length) doUpload(files)
  fileInput.value.value = ''
}

function onDrop(e) {
  isDragging.value = false
  const files = [...e.dataTransfer.files]
  if (files.length) doUpload(files)
}

async function doUpload(files) {
  const allowed = ['.pdf','.doc','.docx','.ppt','.pptx','.xls','.xlsx',
                   '.mp4','.webm','.mp3','.png','.jpg','.jpeg','.gif','.svg','.zip','.rar','.txt']
  const maxSize = 100 * 1024 * 1024

  uploadError.value = ''
  uploading.value = true
  uploadQueue.value = files.map(f => f.name)

  for (const file of files) {
    const ext = '.' + file.name.split('.').pop().toLowerCase()
    if (!allowed.includes(ext)) {
      uploadError.value = `Format tidak didukung: ${file.name}`
      uploadQueue.value = uploadQueue.value.filter(n => n !== file.name)
      continue
    }
    if (file.size > maxSize) {
      uploadError.value = `File terlalu besar (maks 100 MB): ${file.name}`
      uploadQueue.value = uploadQueue.value.filter(n => n !== file.name)
      continue
    }
    try {
      const { data } = await resourcesApi.upload(route.params.id, file)
      resources.value.push(data)
    } catch (e) {
      uploadError.value = e.response?.data?.message || `Gagal upload: ${file.name}`
    }
    uploadQueue.value = uploadQueue.value.filter(n => n !== file.name)
  }

  uploading.value = false
}

async function toggleVisibility(r) {
  const updated = { ...r, isVisible: !r.isVisible }
  await resourcesApi.update(route.params.id, r.id, {
    title: r.title, description: r.description, isVisible: !r.isVisible, order: r.order
  })
  r.isVisible = !r.isVisible
}

async function deleteResource(r) {
  if (!confirm(`Hapus "${r.title}"?`)) return
  await resourcesApi.delete(route.params.id, r.id)
  resources.value = resources.value.filter(x => x.id !== r.id)
}

function fileIconClass(type) {
  return {
    pdf:          'bg-red-100 text-red-600',
    video:        'bg-purple-100 text-purple-600',
    audio:        'bg-pink-100 text-pink-600',
    doc:          'bg-blue-100 text-blue-600',
    presentation: 'bg-orange-100 text-orange-600',
    spreadsheet:  'bg-green-100 text-green-600',
    image:        'bg-yellow-100 text-yellow-600',
    archive:      'bg-gray-100 text-gray-600',
  }[type] ?? 'bg-gray-100 text-gray-600'
}

// ── Init ──────────────────────────────────────────────────────────────────────

onMounted(async () => {
  if (isEdit.value) {
    const { data } = await coursesApi.getById(route.params.id)
    form.value = {
      title: data.title,
      description: data.description,
      category: data.category ?? '',
      level: data.level,
      isPublished: data.isPublished,
      thumbnailUrl: data.thumbnailUrl
    }
    await loadResources()
  }
})
</script>

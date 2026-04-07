<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-2xl font-bold text-gray-900">{{ isEdit ? 'Edit Kursus' : 'Buat Kursus Baru' }}</h1>
    </div>

    <AlertMessage :message="error" type="error" @dismiss="error = null" class="mb-4" />
    <AlertMessage :message="success" type="success" @dismiss="success = null" class="mb-4" />

    <div class="card p-6">
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
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { coursesApi } from '@/api/courses'
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

const commonCategories = ['Pemrograman', 'Desain', 'Bisnis', 'Bahasa', 'Matematika', 'Sains', 'Seni', 'Olahraga']

const form = ref({
  title: '', description: '', category: '', level: 'Semua', isPublished: false, thumbnailUrl: null
})

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

    // Upload thumbnail if new file selected
    if (thumbnailFile.value) {
      await coursesApi.uploadThumbnail(courseId, thumbnailFile.value)
    }

    router.push(`/courses/${courseId}`)
  } catch (e) {
    error.value = e.response?.data?.message || 'Gagal menyimpan kursus.'
  } finally {
    loading.value = false
  }
}

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
  }
})
</script>

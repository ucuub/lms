<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-2xl font-bold text-gray-900">Thread Baru</h1>
    </div>

    <div class="card p-6">
      <form @submit.prevent="submit" class="space-y-4">
        <div>
          <label class="label">Judul *</label>
          <input v-model="form.title" type="text" class="input" placeholder="Tulis judul diskusi..." required maxlength="300" />
        </div>
        <div>
          <label class="label">Isi Diskusi *</label>
          <textarea v-model="form.body" class="textarea" rows="6" placeholder="Jelaskan pertanyaan atau topik diskusimu..." required></textarea>
        </div>
        <div v-if="error" class="text-sm text-red-600 bg-red-50 rounded-lg p-3">{{ error }}</div>
        <div class="flex gap-3">
          <button type="submit" :disabled="loading" class="btn-primary">
            {{ loading ? 'Memposting...' : 'Posting Thread' }}
          </button>
          <button type="button" @click="$router.back()" class="btn-outline">Batal</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { forumApi } from '@/api/forum'

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const error = ref(null)
const form = ref({ title: '', body: '' })

async function submit() {
  loading.value = true
  error.value = null
  try {
    const { data } = await forumApi.createThread(route.params.courseId, form.value)
    router.push(`/courses/${route.params.courseId}/forum/${data.id}`)
  } catch (e) {
    error.value = e.response?.data?.message || 'Gagal memposting thread.'
  } finally {
    loading.value = false
  }
}
</script>

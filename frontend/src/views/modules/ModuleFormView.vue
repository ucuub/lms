<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      <h1 class="text-xl font-bold">{{ isEdit ? 'Edit Modul' : 'Buat Modul Baru' }}</h1>
    </div>
    <div class="card p-6">
      <form @submit.prevent="save" class="space-y-4">
        <div><label class="label">Judul</label><input v-model="form.title" class="input" required /></div>
        <div><label class="label">Konten (HTML)</label><textarea v-model="form.content" class="textarea" rows="8" placeholder="Isi materi..."></textarea></div>
        <div><label class="label">URL Video (YouTube/Vimeo)</label><input v-model="form.videoUrl" class="input" placeholder="https://youtube.com/watch?v=..." /></div>
        <div class="grid grid-cols-2 gap-4">
          <div><label class="label">Urutan</label><input v-model.number="form.order" type="number" class="input" min="0" /></div>
          <div><label class="label">Durasi (menit)</label><input v-model.number="form.durationMinutes" type="number" class="input" min="0" /></div>
        </div>
        <div class="flex items-center gap-2"><input v-model="form.isPublished" type="checkbox" class="w-4 h-4" id="pub"/><label for="pub" class="text-sm">Publikasikan</label></div>
        <div class="flex gap-3"><button type="submit" :disabled="saving" class="btn-primary">{{ saving ? 'Menyimpan...' : 'Simpan' }}</button><button type="button" @click="$router.back()" class="btn-outline">Batal</button></div>
      </form>
    </div>
  </div>
</template>
<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { modulesApi } from '@/api/courses'
const route = useRoute(), router = useRouter()
const isEdit = computed(() => !!route.params.id && route.params.id !== 'create')
const saving = ref(false)
const form = ref({ title: '', content: '', videoUrl: '', order: 0, isPublished: true, durationMinutes: 0 })
async function save() {
  saving.value = true
  try {
    if (isEdit.value) await modulesApi.update(route.params.courseId, route.params.id, form.value)
    else await modulesApi.create(route.params.courseId, form.value)
    router.push(`/courses/${route.params.courseId}`)
  } finally { saving.value = false }
}
onMounted(async () => {
  if (isEdit.value) {
    const { data } = await modulesApi.getById(route.params.courseId, route.params.id)
    form.value = { title: data.title, content: data.content ?? '', videoUrl: data.videoUrl ?? '', order: data.order, isPublished: data.isPublished, durationMinutes: data.durationMinutes }
  }
})
</script>

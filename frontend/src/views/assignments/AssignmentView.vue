<template>
  <div class="max-w-2xl mx-auto p-6">
    <button @click="$router.back()" class="btn-outline btn-sm mb-4">← Kembali</button>
    <div v-if="assignment" class="space-y-4">
      <div class="card p-6">
        <h1 class="text-xl font-bold mb-2">{{ assignment.title }}</h1>
        <p class="text-sm text-gray-500 mb-3">Deadline: {{ assignment.dueDate ? new Date(assignment.dueDate).toLocaleString('id-ID') : 'Tidak ada' }} · Max: {{ assignment.maxScore }}</p>
        <p class="text-gray-700 whitespace-pre-line">{{ assignment.description }}</p>
      </div>
      <div v-if="!auth.isTeacher" class="card p-6">
        <h2 class="font-semibold mb-3">{{ mySubmission ? 'Update Submission' : 'Submit Tugas' }}</h2>
        <div v-if="mySubmission && mySubmission.status === 'Graded'" class="alert-success mb-3">Nilai: {{ mySubmission.score }} / {{ assignment.maxScore }} — {{ mySubmission.feedback }}</div>
        <form @submit.prevent="submit" class="space-y-3">
          <textarea v-model="form.text" class="textarea" rows="5" placeholder="Tulis jawaban..."></textarea>
          <div><label class="label">Atau upload file</label><input type="file" ref="fileInput" class="text-sm" /></div>
          <button type="submit" :disabled="submitting" class="btn-primary">{{ submitting ? 'Mengirim...' : 'Kirim' }}</button>
        </form>
      </div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { assignmentsApi } from '@/api/assignments'
const route = useRoute(), auth = useAuthStore()
const assignment = ref(null), mySubmission = ref(null), submitting = ref(false)
const form = ref({ text: '' }), fileInput = ref(null)
async function submit() {
  submitting.value = true
  const file = fileInput.value?.files[0]
  if (fileInput.value) fileInput.value.value = '' // reset agar file yang sama bisa dipilih lagi
  try {
    const { data } = await assignmentsApi.submit(route.params.id, form.value.text, file)
    mySubmission.value = data
  } finally { submitting.value = false }
}
onMounted(async () => {
  const { data } = await assignmentsApi.getById(route.params.id)
  assignment.value = data
  try { const r = await assignmentsApi.mySubmission(route.params.id); mySubmission.value = r.data } catch {}
})
</script>

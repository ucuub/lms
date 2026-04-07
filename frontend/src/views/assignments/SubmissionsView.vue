<template>
  <div class="p-6 max-w-5xl mx-auto">
    <button @click="$router.back()" class="btn-outline btn-sm mb-4">← Kembali</button>
    <h1 class="text-xl font-bold mb-6">Submissions: {{ assignmentTitle }}</h1>
    <div class="table-wrap">
      <table>
        <thead><tr><th>Siswa</th><th>Tipe</th><th>Status</th><th>Nilai</th><th>Aksi</th></tr></thead>
        <tbody>
          <tr v-for="s in submissions" :key="s.id">
            <td>{{ s.userName }}</td>
            <td><a v-if="s.fileUrl" :href="s.fileUrl" target="_blank" class="text-blue-600 text-xs hover:underline">📎 File</a><span v-else class="text-xs text-gray-500">Teks</span></td>
            <td><span :class="s.status === 'Graded' ? 'badge-green' : 'badge-blue'" class="badge">{{ s.status }}</span></td>
            <td>{{ s.score != null ? s.score : '-' }}</td>
            <td>
              <div class="flex gap-2 items-center">
                <input v-model.number="grading[s.id].score" type="number" class="input w-16 text-sm" :max="maxScore" min="0" />
                <button @click="grade(s.id)" class="btn-primary btn-sm">Nilai</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { assignmentsApi } from '@/api/assignments'
const route = useRoute()
const submissions = ref([]), assignmentTitle = ref(''), maxScore = ref(100)
const grading = reactive({})
async function grade(id) {
  await assignmentsApi.gradeSubmission(id, grading[id])
  const { data } = await assignmentsApi.getSubmissions(route.params.id)
  submissions.value = data
}
onMounted(async () => {
  const [subRes, aRes] = await Promise.all([
    assignmentsApi.getSubmissions(route.params.id),
    assignmentsApi.getById(route.params.id)
  ])
  submissions.value = subRes.data
  assignmentTitle.value = aRes.data.title
  maxScore.value = aRes.data.maxScore
  subRes.data.forEach(s => { grading[s.id] = { score: s.score ?? 0, feedback: s.feedback ?? '' } })
})
</script>

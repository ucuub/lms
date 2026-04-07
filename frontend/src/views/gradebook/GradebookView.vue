<template>
  <div class="p-6 max-w-6xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Gradebook</h1>
        <p class="text-gray-500 text-sm">{{ courseTitle }}</p>
      </div>
      <div class="flex gap-2">
        <button v-if="auth.isTeacher" @click="exportCsv" class="btn-outline btn-sm">
          ⬇ Export CSV
        </button>
        <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
      </div>
    </div>

    <!-- Teacher: all students -->
    <div v-if="auth.isTeacher">
      <div v-if="loading" class="flex justify-center py-12">
        <div class="animate-spin rounded-full h-8 w-8 border-2 border-blue-600 border-t-transparent"></div>
      </div>
      <div v-else class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Nama Siswa</th>
              <th v-for="a in assignmentCols" :key="'a'+a.id" class="whitespace-nowrap">📝 {{ a.title }}</th>
              <th v-for="q in quizCols" :key="'q'+q.id" class="whitespace-nowrap">❓ {{ q.title }}</th>
              <th>Total %</th>
              <th>Grade</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in rows" :key="row.userId">
              <td class="font-medium">{{ row.userName }}</td>
              <td v-for="item in row.assignments" :key="item.id">
                <span :class="gradeColor(item)">{{ item.score ?? '-' }} / {{ item.maxScore }}</span>
              </td>
              <td v-for="item in row.quizzes" :key="item.id">
                <span :class="gradeColor(item)">{{ item.score != null ? Math.round(item.score / item.maxScore * 100) + '%' : '-' }}</span>
              </td>
              <td class="font-medium">{{ row.totalPercentage != null ? row.totalPercentage + '%' : '-' }}</td>
              <td><span :class="letterGradeColor(row.letterGrade)" class="badge">{{ row.letterGrade }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Student: my grades -->
    <div v-else>
      <div v-if="myGrades" class="space-y-6">
        <!-- Overall -->
        <div class="card p-6 text-center">
          <div :class="['text-5xl font-bold', myGrades.totalPercentage >= 60 ? 'text-green-600' : 'text-red-600']">
            {{ myGrades.letterGrade }}
          </div>
          <p class="text-gray-500 mt-1">{{ myGrades.totalPercentage != null ? myGrades.totalPercentage + '%' : 'Belum ada nilai' }}</p>
        </div>

        <!-- Assignments -->
        <div v-if="myGrades.assignments.length" class="card p-6">
          <h2 class="font-semibold mb-4">Tugas</h2>
          <div class="space-y-3">
            <div v-for="item in myGrades.assignments" :key="item.id" class="flex items-center justify-between">
              <span class="text-sm text-gray-700">{{ item.title }}</span>
              <div class="flex items-center gap-3">
                <span :class="['text-sm font-medium', gradeColor(item)]">
                  {{ item.score != null ? `${item.score} / ${item.maxScore}` : 'Belum dinilai' }}
                </span>
                <span :class="statusBadge(item.status)" class="badge">{{ statusLabel(item.status) }}</span>
              </div>
            </div>
          </div>
        </div>

        <!-- Quizzes -->
        <div v-if="myGrades.quizzes.length" class="card p-6">
          <h2 class="font-semibold mb-4">Quiz</h2>
          <div class="space-y-3">
            <div v-for="item in myGrades.quizzes" :key="item.id" class="flex items-center justify-between">
              <span class="text-sm text-gray-700">{{ item.title }}</span>
              <span :class="['text-sm font-medium', gradeColor(item)]">
                {{ item.score != null ? Math.round(item.score / item.maxScore * 100) + '%' : '-' }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { gradebookApi } from '@/api/assignments'

const route = useRoute()
const auth = useAuthStore()
const loading = ref(true)
const rows = ref([])
const myGrades = ref(null)
const courseTitle = ref('')

const assignmentCols = computed(() => rows.value[0]?.assignments ?? [])
const quizCols = computed(() => rows.value[0]?.quizzes ?? [])

function gradeColor(item) {
  if (item.score == null) return 'text-gray-400'
  const pct = item.score / item.maxScore * 100
  if (pct >= 80) return 'text-green-600'
  if (pct >= 60) return 'text-yellow-600'
  return 'text-red-600'
}

function letterGradeColor(g) {
  const map = { A: 'badge-green', B: 'badge-blue', C: 'badge-yellow', D: 'badge-yellow', E: 'badge-red' }
  return map[g] ?? 'badge-gray'
}

function statusBadge(s) {
  return { graded: 'badge-green', submitted: 'badge-blue', not_submitted: 'badge-gray' }[s] ?? 'badge-gray'
}

function statusLabel(s) {
  return { graded: 'Dinilai', submitted: 'Dikirim', not_submitted: 'Belum' }[s] ?? s
}

async function exportCsv() {
  const { data } = await gradebookApi.exportCsv(route.params.courseId)
  const url = URL.createObjectURL(data)
  const a = document.createElement('a')
  a.href = url
  a.download = `gradebook-${route.params.courseId}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

onMounted(async () => {
  try {
    if (auth.isTeacher) {
      const { data } = await gradebookApi.getCourse(route.params.courseId)
      rows.value = data
    } else {
      const { data } = await gradebookApi.getMine(route.params.courseId)
      myGrades.value = data
      courseTitle.value = data.courseTitle
    }
  } finally {
    loading.value = false
  }
})
</script>

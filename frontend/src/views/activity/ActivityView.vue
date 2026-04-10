<template>
  <div class="max-w-2xl mx-auto p-6">
    <div class="flex items-center gap-3 mb-6">
      <h1 class="text-2xl font-bold text-gray-900">Riwayat Aktivitas</h1>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-2 border-blue-600 border-t-transparent"></div>
    </div>

    <div v-else-if="activities.length === 0" class="card p-12 text-center text-gray-400">
      Belum ada aktivitas yang tercatat.
    </div>

    <div v-else class="relative">
      <!-- Timeline line -->
      <div class="absolute left-5 top-2 bottom-2 w-0.5 bg-gray-100"></div>

      <div class="space-y-4">
        <div v-for="act in activities" :key="act.id" class="flex gap-4 relative">
          <!-- Dot -->
          <div class="w-10 h-10 rounded-full flex items-center justify-center shrink-0 z-10"
            :class="iconBg(act.action)">
            <span class="text-base">{{ icon(act.action) }}</span>
          </div>

          <div class="card p-4 flex-1">
            <div class="flex items-start justify-between gap-2">
              <div>
                <p class="text-sm font-medium text-gray-900">{{ label(act) }}</p>
                <p v-if="act.entityTitle" class="text-xs text-gray-500 mt-0.5">{{ act.entityTitle }}</p>
              </div>
              <time class="text-xs text-gray-400 shrink-0">{{ formatTime(act.timestamp) }}</time>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { activityApi } from '@/api/activity'

const activities = ref([])
const loading = ref(true)

const ACTION_LABELS = {
  view_module: 'Melihat modul',
  submit_assignment: 'Mengumpulkan tugas',
  start_quiz: 'Memulai kuis',
  complete_quiz: 'Menyelesaikan kuis',
  enroll_course: 'Mendaftar kursus',
  complete_course: 'Menyelesaikan kursus',
}

const ACTION_ICONS = {
  view_module: '📖',
  submit_assignment: '📝',
  start_quiz: '🧩',
  complete_quiz: '✅',
  enroll_course: '🎓',
  complete_course: '🏆',
}

const ACTION_BG = {
  view_module: 'bg-blue-100',
  submit_assignment: 'bg-yellow-100',
  start_quiz: 'bg-purple-100',
  complete_quiz: 'bg-green-100',
  enroll_course: 'bg-indigo-100',
  complete_course: 'bg-orange-100',
}

function icon(action) {
  return ACTION_ICONS[action] ?? '📌'
}

function iconBg(action) {
  return ACTION_BG[action] ?? 'bg-gray-100'
}

function label(act) {
  return ACTION_LABELS[act.action] ?? act.action
}

function formatTime(dt) {
  if (!dt) return ''
  const d = new Date(dt)
  return d.toLocaleString('id-ID', {
    day: 'numeric', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  })
}

onMounted(async () => {
  try {
    const { data } = await activityApi.getMyActivity()
    activities.value = data
  } finally {
    loading.value = false
  }
})
</script>

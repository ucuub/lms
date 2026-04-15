<template>
  <div class="p-6 max-w-5xl mx-auto">
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-gray-900">Ujian</h1>
        <p class="text-sm text-gray-500 mt-1">Kerjakan ujian yang tersedia</p>
      </div>
      <RouterLink v-if="auth.isTeacher" to="/ujian/create"
        class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
        </svg>
        Buat Ujian
      </RouterLink>
    </div>

    <div v-if="loading" class="text-center py-12 text-gray-400">Memuat...</div>

    <div v-else-if="sets.length === 0" class="text-center py-12">
      <svg class="w-12 h-12 text-gray-300 mx-auto mb-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
      </svg>
      <p class="text-gray-500">Belum ada paket soal tersedia.</p>
    </div>

    <div v-else class="grid gap-4 sm:grid-cols-2">
      <div v-for="qs in sets" :key="qs.id"
        class="bg-white rounded-xl border border-gray-200 p-5 flex flex-col gap-3 hover:shadow-md transition-shadow">
        <!-- Header -->
        <div class="flex items-start justify-between gap-2">
          <div class="flex-1 min-w-0">
            <h3 class="font-semibold text-gray-900 truncate">{{ qs.title }}</h3>
            <p v-if="qs.description" class="text-sm text-gray-500 line-clamp-2 mt-0.5">{{ qs.description }}</p>
          </div>
          <span :class="qs.isPublished
            ? 'bg-green-100 text-green-700'
            : 'bg-yellow-100 text-yellow-700'"
            class="text-xs font-medium px-2 py-0.5 rounded-full shrink-0">
            {{ qs.isPublished ? 'Published' : 'Draft' }}
          </span>
        </div>

        <!-- Stats -->
        <div class="flex items-center gap-4 text-sm text-gray-500">
          <span class="flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            {{ qs.questionCount }} soal
          </span>
          <span class="flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            {{ qs.timeLimitMinutes ? `${qs.timeLimitMinutes} menit` : 'Tanpa batas waktu' }}
          </span>
          <span class="flex items-center gap-1">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
            Lulus ≥{{ qs.passScore }}%
          </span>
        </div>

        <!-- Attempt info -->
        <div class="text-xs text-gray-400">
          <span>Percobaan: {{ qs.myAttemptCount ?? 0 }} / {{ qs.maxAttempts }}</span>
          <span class="mx-2">·</span>
          <span>Oleh {{ qs.createdByName }}</span>
        </div>

        <!-- Actions -->
        <div class="flex gap-2 pt-1">
          <RouterLink :to="`/ujian/${qs.id}/take`"
            v-if="qs.isPublished && (qs.myAttemptCount ?? 0) < qs.maxAttempts"
            class="flex-1 text-center py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors">
            Kerjakan
          </RouterLink>
          <span v-else-if="qs.isPublished"
            class="flex-1 text-center py-2 bg-gray-100 text-gray-400 text-sm font-medium rounded-lg cursor-not-allowed">
            Percobaan habis
          </span>

          <RouterLink v-if="auth.isTeacher && (auth.isAdmin || qs.createdBy === auth.user?.id)"
            :to="`/ujian/${qs.id}/manage`"
            class="px-3 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            Kelola
          </RouterLink>
          <RouterLink v-if="auth.isTeacher && (auth.isAdmin || qs.createdBy === auth.user?.id)"
            :to="`/ujian/${qs.id}/grading`"
            class="px-3 py-2 text-sm font-medium text-gray-600 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
            Nilai
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { questionSetsApi } from '@/api/questionSets'

const auth = useAuthStore()
const sets = ref([])
const loading = ref(true)

onMounted(async () => {
  try {
    const { data } = await questionSetsApi.getAll()
    sets.value = data
  } finally {
    loading.value = false
  }
})
</script>

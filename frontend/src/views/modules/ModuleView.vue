<template>
  <div class="max-w-4xl mx-auto p-6">
    <div v-if="module">
      <div class="flex items-center justify-between gap-3 mb-6">
        <div class="flex items-center gap-3">
          <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
          <div>
            <h1 class="text-xl font-bold text-gray-900">{{ module.title }}</h1>
            <p class="text-sm text-gray-500">{{ module.contentType }}</p>
          </div>
        </div>
        <!-- Edit button — hanya untuk teacher/admin -->
        <RouterLink v-if="auth.isTeacher"
          :to="`/courses/${route.params.courseId}/modules/${route.params.id}/edit`"
          class="btn-outline btn-sm shrink-0">
          ✏️ Edit Modul
        </RouterLink>
      </div>
      <div class="card p-6 space-y-6">
        <!-- Video embed (YouTube / Vimeo) -->
        <div v-if="module.videoEmbedId" class="aspect-video rounded-lg overflow-hidden bg-gray-900">
          <iframe v-if="module.videoProvider === 'YouTube'"
            :src="`https://www.youtube.com/embed/${module.videoEmbedId}?rel=0`"
            class="w-full h-full" allowfullscreen frameborder="0"
            title="Video pembelajaran"></iframe>
          <iframe v-else-if="module.videoProvider === 'Vimeo'"
            :src="`https://player.vimeo.com/video/${module.videoEmbedId}`"
            class="w-full h-full" allowfullscreen frameborder="0"
            title="Video pembelajaran"></iframe>
        </div>
        <!-- Fallback: URL video yang tidak bisa di-embed -->
        <div v-else-if="module.videoUrl" class="p-4 bg-blue-50 rounded-lg flex items-center gap-3">
          <svg class="w-5 h-5 text-blue-500 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"/>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <a :href="module.videoUrl" target="_blank" rel="noopener"
            class="text-blue-600 hover:underline text-sm font-medium truncate">
            {{ module.videoUrl }}
          </a>
        </div>
        <div v-if="module.content" class="prose max-w-none" v-html="module.content"></div>
        <div v-if="module.attachments?.length">
          <h3 class="font-medium mb-3">Lampiran</h3>
          <div class="space-y-2">
            <a v-for="att in module.attachments" :key="att.id"
              :href="att.fileUrl" target="_blank" rel="noopener"
              class="flex items-center justify-between p-3 rounded-lg border border-gray-200 hover:bg-gray-50 hover:border-blue-300 transition group">
              <div class="flex items-center gap-3 min-w-0">
                <div class="w-9 h-9 rounded bg-blue-100 text-blue-600 flex items-center justify-center text-xs font-bold uppercase shrink-0">
                  {{ att.fileType }}
                </div>
                <div class="min-w-0">
                  <p class="text-sm font-medium text-gray-800 truncate group-hover:text-blue-600">{{ att.fileName }}</p>
                  <p class="text-xs text-gray-400">{{ formatSize(att.fileSize) }}</p>
                </div>
              </div>
              <svg class="w-4 h-4 text-gray-400 group-hover:text-blue-500 shrink-0 ml-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
              </svg>
            </a>
          </div>
        </div>
      </div>
    </div>
    <div v-else class="flex justify-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-2 border-blue-600 border-t-transparent"></div>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { modulesApi } from '@/api/courses'
import { useAuthStore } from '@/stores/auth'
const route = useRoute()
const auth = useAuthStore()
const module = ref(null)

function formatSize(bytes) {
  if (bytes >= 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
  if (bytes >= 1024) return `${Math.round(bytes / 1024)} KB`
  return `${bytes} B`
}

onMounted(async () => {
  try {
    const { data } = await modulesApi.getById(route.params.courseId, route.params.id)
    module.value = data
  } catch (e) {
    console.error('[ModuleView] Failed to load module:', e?.response?.status, e?.message)
  }
})
</script>

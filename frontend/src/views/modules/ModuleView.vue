<template>
  <div class="max-w-4xl mx-auto p-6">
    <div v-if="module">
      <div class="flex items-center justify-between gap-3 mb-6">
        <div class="flex items-center gap-3">
          <button @click="$router.back()" class="btn-outline btn-sm">← Kembali</button>
          <div>
            <h1 class="text-xl font-bold text-gray-900">{{ module.title }}</h1>
            <p class="text-sm text-gray-500">{{ module.durationMinutes }}m · {{ module.contentType }}</p>
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
        <div v-if="module.videoEmbedId" class="aspect-video rounded-lg overflow-hidden bg-gray-900">
          <iframe v-if="module.videoProvider === 'YouTube'"
            :src="`https://www.youtube.com/embed/${module.videoEmbedId}?rel=0`"
            class="w-full h-full" allowfullscreen frameborder="0"></iframe>
          <iframe v-else-if="module.videoProvider === 'Vimeo'"
            :src="`https://player.vimeo.com/video/${module.videoEmbedId}`"
            class="w-full h-full" allowfullscreen frameborder="0"></iframe>
        </div>
        <div v-if="module.content" class="prose max-w-none" v-html="module.content"></div>
        <div v-if="module.attachments?.length">
          <h3 class="font-medium mb-2">Lampiran</h3>
          <div class="space-y-2">
            <a v-for="att in module.attachments" :key="att.id" :href="att.fileUrl" target="_blank"
              class="flex items-center gap-2 p-2 rounded border border-gray-200 hover:bg-gray-50 text-sm text-blue-600 hover:underline">
              📎 {{ att.fileName }}
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
onMounted(async () => {
  const { data } = await modulesApi.getById(route.params.courseId, route.params.id)
  module.value = data
})
</script>

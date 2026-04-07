<template>
  <div class="max-w-4xl mx-auto p-6">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold">Forum Diskusi</h1>
      <RouterLink :to="`/courses/${route.params.courseId}/forum/create`" class="btn-primary">+ Thread Baru</RouterLink>
    </div>
    <div class="space-y-3">
      <div v-for="t in threads" :key="t.id" class="card p-4 hover:shadow-md transition cursor-pointer" @click="$router.push(`/courses/${route.params.courseId}/forum/${t.id}`)">
        <div class="flex items-start justify-between">
          <div>
            <div class="flex items-center gap-2 mb-1">
              <span v-if="t.isPinned" class="text-xs text-yellow-600">📌</span>
              <h3 class="font-medium text-gray-900">{{ t.title }}</h3>
            </div>
            <p class="text-sm text-gray-500">{{ t.userName }} · {{ new Date(t.createdAt).toLocaleDateString('id-ID') }}</p>
          </div>
          <span class="text-sm text-gray-500">{{ t.replyCount }} balasan</span>
        </div>
      </div>
    </div>
    <Pagination :model-value="page" :total-pages="totalPages" @change="p => { page = p; load() }" />
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { forumApi } from '@/api/forum'
import Pagination from '@/components/common/Pagination.vue'
const route = useRoute()
const threads = ref([]), page = ref(1), totalPages = ref(1)
async function load() {
  const { data } = await forumApi.getThreads(route.params.courseId, { page: page.value })
  threads.value = data.items
  totalPages.value = data.totalPages
}
onMounted(load)
</script>

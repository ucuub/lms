<template>
  <div class="max-w-4xl mx-auto p-6">
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-bold">Forum Diskusi</h1>
      <RouterLink :to="`/courses/${route.params.courseId}/forum/create`" class="btn-primary">+ Thread Baru</RouterLink>
    </div>
    <div class="space-y-3">
      <div v-for="t in threads" :key="t.id" class="card p-4 hover:shadow-md transition">
        <div class="flex items-start justify-between gap-3 cursor-pointer" @click="$router.push(`/courses/${route.params.courseId}/forum/${t.id}`)">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 mb-1">
              <span v-if="t.isPinned" class="text-xs text-yellow-600">📌</span>
              <h3 class="font-medium text-gray-900 truncate">{{ t.title }}</h3>
            </div>
            <p class="text-sm text-gray-500">{{ t.userName }} · {{ new Date(t.createdAt).toLocaleDateString('id-ID') }}</p>
          </div>
          <div class="flex items-center gap-3 shrink-0">
            <span class="text-sm text-gray-500">{{ t.replyCount }} balasan</span>
            <button
              v-if="canDelete(t)"
              @click.stop="deleteThread(t)"
              class="text-xs text-red-400 hover:text-red-600 px-2 py-1 rounded hover:bg-red-50 transition-colors">
              Hapus
            </button>
          </div>
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
import { useAuthStore } from '@/stores/auth'
import Pagination from '@/components/common/Pagination.vue'

const route = useRoute()
const auth = useAuthStore()
const threads = ref([])
const page = ref(1)
const totalPages = ref(1)

function canDelete(t) {
  if (auth.isTeacher || auth.isAdmin) return true
  return t.userId === auth.user?.id || t.authorId === auth.user?.id
}

async function load() {
  const { data } = await forumApi.getThreads(route.params.courseId, { page: page.value })
  threads.value = data.items
  totalPages.value = data.totalPages
}

async function deleteThread(t) {
  if (!confirm(`Hapus thread "${t.title}"? Semua balasan juga akan terhapus.`)) return
  try {
    await forumApi.delete(route.params.courseId, t.id)
    threads.value = threads.value.filter(x => x.id !== t.id)
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus thread.')
  }
}

onMounted(load)
</script>

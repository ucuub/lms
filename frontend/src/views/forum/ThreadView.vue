<template>
  <div class="max-w-3xl mx-auto p-6">
    <button @click="$router.back()" class="btn-outline btn-sm mb-4">← Forum</button>
    <div v-if="thread" class="space-y-4">
      <!-- Thread post -->
      <div class="card p-6">
        <div class="flex items-start justify-between gap-3 mb-1">
          <h1 class="text-xl font-bold">{{ thread.title }}</h1>
          <button
            v-if="canDelete(thread)"
            @click="deletePost(thread.id, true)"
            class="text-xs text-red-400 hover:text-red-600 shrink-0 mt-1">
            Hapus
          </button>
        </div>
        <p class="text-sm text-gray-500 mb-4">{{ thread.userName }} · {{ new Date(thread.createdAt).toLocaleDateString('id-ID') }}</p>
        <p class="text-gray-700 whitespace-pre-line">{{ thread.body }}</p>
      </div>

      <!-- Replies -->
      <div class="card p-6">
        <h2 class="font-semibold mb-4">{{ thread.replies?.length ?? 0 }} Balasan</h2>
        <div class="space-y-4 mb-6">
          <div v-for="r in thread.replies" :key="r.id" class="border-l-2 border-blue-200 pl-4">
            <div class="flex items-center justify-between gap-2 mb-1">
              <p class="text-xs text-gray-500">{{ r.userName }} · {{ new Date(r.createdAt).toLocaleDateString('id-ID') }}</p>
              <button
                v-if="canDelete(r)"
                @click="deletePost(r.id, false)"
                class="text-xs text-red-400 hover:text-red-600 shrink-0">
                Hapus
              </button>
            </div>
            <p class="text-sm text-gray-800 whitespace-pre-line">{{ r.body }}</p>
          </div>
        </div>
        <form @submit.prevent="reply" class="space-y-2">
          <textarea v-model="replyText" class="textarea" rows="3" placeholder="Tulis balasan..." required></textarea>
          <button type="submit" class="btn-primary btn-sm">Kirim Balasan</button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { forumApi } from '@/api/forum'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const thread = ref(null)
const replyText = ref('')

function canDelete(post) {
  if (!post || post.isDeleted) return false
  if (auth.isTeacher || auth.isAdmin) return true
  return post.userId === auth.user?.id || post.authorId === auth.user?.id
}

async function load() {
  const { data } = await forumApi.getThread(route.params.courseId, route.params.threadId)
  thread.value = data
}

async function reply() {
  await forumApi.reply(route.params.courseId, route.params.threadId, { body: replyText.value })
  replyText.value = ''
  load()
}

async function deletePost(postId, isThread) {
  const msg = isThread
    ? 'Hapus thread ini? Semua balasan juga akan terhapus.'
    : 'Hapus balasan ini?'
  if (!confirm(msg)) return
  try {
    await forumApi.delete(route.params.courseId, postId)
    if (isThread) {
      router.back()
    } else {
      await load()
    }
  } catch (e) {
    alert(e?.response?.data?.message ?? 'Gagal menghapus pesan.')
  }
}

onMounted(load)
</script>
